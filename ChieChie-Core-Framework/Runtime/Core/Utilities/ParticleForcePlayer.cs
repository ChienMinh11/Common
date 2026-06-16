using UnityEngine;

namespace ChieChie.Core
{
    public class ParticleForcePlayer : MonoBehaviour
    {
        [Header("Particle Settings")]
        public UnityEngine.ParticleSystem[] particleSystems;
        public bool includeChildParticles = true;
        public bool autoFindParticles = true;
   
        [Header("Play Options")]
        public bool stopBeforePlay = true;
        public bool clearBeforePlay = true;
   
        [Header("Auto Play")]
        public bool playOnEnable = true;


        void Start()
        {
            if (autoFindParticles)
            {
                FindAllParticles();
            }
        }
   
        void OnEnable()
        {
            if (playOnEnable)
            {
                // Delay một frame để đảm bảo particle system đã sẵn sàng
                StartCoroutine(PlayOnEnableCoroutine());
            }
        }
   
        System.Collections.IEnumerator PlayOnEnableCoroutine()
        {
            yield return null; // Wait 1 frame
            ForcePlayAll();
        }
   
        void FindAllParticles()
        {
            particleSystems = GetComponentsInChildren<UnityEngine.ParticleSystem>();
        }
   
        public void ForcePlayAll()
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                FindAllParticles();
            }
       
            foreach (var particle in particleSystems)
            {
                if (particle != null)
                {
                    ForcePlaySingle(particle);
                }
            }
        }
   
        public void ForcePlaySingle(UnityEngine.ParticleSystem particle)
        {
            if (particle == null) return;
       
            if (stopBeforePlay)
            {
                particle.Stop();
            }
       
            if (clearBeforePlay)
            {
                particle.Clear();
            }
       
            particle.Play(includeChildParticles);
        }
   
        public void ForceStopAll()
        {
            foreach (var particle in particleSystems)
            {
                if (particle != null)
                {
                    particle.Stop();
                    particle.Clear();
                }
            }
        }
   
        // Gọi từ UI Button hoặc Event
        public void OnForcePlay()
        {
            ForcePlayAll();
        }
    }
}
