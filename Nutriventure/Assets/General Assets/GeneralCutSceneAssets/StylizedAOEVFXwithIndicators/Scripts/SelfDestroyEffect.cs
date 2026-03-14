using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace VFXSelfDestroy
{

    public class SelfDestroyEffect : MonoBehaviour
    {
        private VisualEffect effect;
        private bool effectPlayed = false;

        void Start()
        {
            effect = gameObject.GetComponent<VisualEffect>();
            effect.Play();
            StartCoroutine(CheckEffectFinished());
        }

        private IEnumerator CheckEffectFinished()
        {
            // Wait until the effect has started emitting
            while (!effectPlayed)
            {
                if (effect.aliveParticleCount > 0)
                    effectPlayed = true;
                yield return new WaitForSeconds(0.25f);
            }

            // Wait until all particles are dead
            while (effect.aliveParticleCount > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            Destroy(gameObject);
        }
    }
}
