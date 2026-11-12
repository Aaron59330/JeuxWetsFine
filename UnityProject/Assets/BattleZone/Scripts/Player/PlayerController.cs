using UnityEngine;

namespace BattleZone
{
    /// <summary>Déplacements FPS : marche, sprint, saut, accroupi, glissade, parachute.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Health health;
        public Transform head;
        public CharacterController cc;
        public WeaponSystem weapons;
        public bool dropping, chute, crouch, sprint;
        public float pitch, yaw;
        public Vector3 Velocity { get { return hv; } }

        Vector3 hv; float vy, slideT, slideSpd, eye = 1.65f;
        Vector3 slideDir;
        WorldBuilder world;
        float lastMoveSpeed;

        public void Init(WorldBuilder w) { world = w; }

        public void AddRecoil(float pitchRad, float yawRad)
        {
            pitch -= pitchRad * Mathf.Rad2Deg;
            yaw += yawRad * Mathf.Rad2Deg;
        }

        static bool K(KeyCode k) { return Input.GetKey(k); }

        void Update()
        {
            if (GameBootstrap.I.State != GameState.Playing) return;
            if (health.dead) return;
            Look();
            Move();
        }

        void Look()
        {
            float k = Cfg.Sens * .03f / (1f + weapons.ads * (weapons.Current.zoom - 1f));
            yaw += Input.GetAxisRaw("Mouse X") * k;
            pitch -= Input.GetAxisRaw("Mouse Y") * k * (Cfg.InvertY ? -1f : 1f);
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            head.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        void Move()
        {
            float dt = Time.deltaTime;
            float f = (K(KeyCode.W) || K(KeyCode.Z) || K(KeyCode.UpArrow) ? 1 : 0) - (K(KeyCode.S) || K(KeyCode.DownArrow) ? 1 : 0);
            float s = (K(KeyCode.D) || K(KeyCode.RightArrow) ? 1 : 0) - (K(KeyCode.A) || K(KeyCode.Q) || K(KeyCode.LeftArrow) ? 1 : 0);
            Vector3 dir = transform.forward * f + transform.right * s;
            if (dir.sqrMagnitude > 1) dir.Normalize();

            if (dropping)
            {
                float ground = world.HeightAt(transform.position.x, transform.position.z);
                if (transform.position.y - ground < 30f) chute = true;
                if (Input.GetKeyDown(KeyCode.Space)) chute = !chute;
                vy = Mathf.Lerp(vy, chute ? -7f : -38f, 1f - Mathf.Exp(-3f * dt));
                hv = Vector3.Lerp(hv, dir * (chute ? 10f : 16f), 1f - Mathf.Exp(-1.5f * dt));
                cc.Move(new Vector3(hv.x, vy, hv.z) * dt);
                if (cc.isGrounded) { dropping = false; chute = false; vy = 0; GameBootstrap.I.UI.Banner("À toi de jouer !", 1.5f); }
                return;
            }

            bool grounded = cc.isGrounded;
            bool wet = transform.position.y < -.2f;
            bool wantCrouch = K(KeyCode.C) || K(KeyCode.LeftControl);
            sprint = (K(KeyCode.LeftShift) || K(KeyCode.RightShift)) && f > 0 && weapons.ads < .2f && !crouch && grounded && !wet && !weapons.FireHeldWithAmmo;

            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
                if (sprint && slideT <= 0 && hv.magnitude > 4f) { slideT = .9f; slideSpd = 11.5f; slideDir = hv.normalized; }

            if (wantCrouch || slideT > 0) crouch = true;
            else if (crouch && !Physics.Raycast(transform.position + Vector3.up * 1.1f, Vector3.up, .8f)) crouch = false;

            float speed = crouch ? 2.8f : sprint ? 8.4f : 5.4f;
            speed *= Mathf.Lerp(1f, .62f, weapons.ads);
            if (wet) speed *= .55f;

            if (slideT > 0)
            {
                slideT -= dt; slideSpd = Mathf.Max(3f, slideSpd - 13f * dt);
                hv = slideDir * slideSpd;
                if (Input.GetKey(KeyCode.Space)) { slideT = 0; vy = 7f; }
            }
            else
            {
                float k = grounded ? 11f : 2.2f;
                hv = Vector3.Lerp(hv, dir * speed, 1f - Mathf.Exp(-k * dt));
            }

            if (grounded && vy < 0) vy = -2f;
            if (Input.GetKey(KeyCode.Space) && grounded && slideT <= 0) vy = 8.2f;
            vy -= 24f * dt;

            // eau profonde et bord de l'île = infranchissables
            Vector3 p = transform.position, next = p + hv * dt * 2f;
            if (world.HeightAt(next.x, next.z) < -2.4f) hv = Vector3.zero;
            if (new Vector2(p.x, p.z).magnitude > WorldBuilder.MAP - 6f && Vector3.Dot(hv, new Vector3(p.x, 0, p.z)) > 0) hv = Vector3.zero;

            cc.Move(new Vector3(hv.x, vy, hv.z) * dt);
            lastMoveSpeed = hv.magnitude;

            // hauteur du personnage / caméra
            float h = crouch ? 1.2f : 1.8f;
            cc.height = Mathf.Lerp(cc.height, h, 1f - Mathf.Exp(-14f * dt));
            cc.center = new Vector3(0, cc.height / 2f, 0);
            eye = Mathf.Lerp(eye, h - .15f, 1f - Mathf.Exp(-14f * dt));
            float bob = grounded ? Mathf.Sin(Time.time * (sprint ? 12f : 8f)) * .025f * Mathf.Clamp01(lastMoveSpeed / 5f) * (1f - weapons.ads) : 0f;
            head.localPosition = new Vector3(0, eye + bob, 0);
        }

        public float MoveSpeed01 { get { return Mathf.Clamp01(lastMoveSpeed / 8f); } }
    }
}
