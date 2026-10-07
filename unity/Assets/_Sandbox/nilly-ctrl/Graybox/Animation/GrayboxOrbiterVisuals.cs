using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Turns the Swarm Nest's orbiting damage wisps into running soldier ants.
    /// </summary>
    /// <remarks>
    /// <see cref="OrbitingDamageField"/> builds its orbiters itself, as plain tinted squares
    /// named "Orbiter N", and moves them every frame. This only dresses those objects: it
    /// gives each one the ant clip and turns it to face the way it is travelling.
    /// </remarks>
    [RequireComponent(typeof(OrbitingDamageField))]
    public class GrayboxOrbiterVisuals : MonoBehaviour
    {
        // World scale of the ant art, whatever size the field gives its orbiters. The art is drawn
        // at real size (a 12 px ant), like everything else.
        private const float AntScale = 1f;

        private Transform[] _orbiters;
        private Vector3[] _last;

        private void LateUpdate()
        {
            if (_orbiters == null && !TryDress())
            {
                return;
            }

            for (int i = 0; i < _orbiters.Length; i++)
            {
                Transform orbiter = _orbiters[i];
                if (orbiter == null)
                {
                    continue;
                }

                Vector3 delta = orbiter.position - _last[i];
                _last[i] = orbiter.position;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    orbiter.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
            }
        }

        private bool TryDress()
        {
            var player = GetComponent<SpriteClipPlayer>();
            SpriteAnimLibrary library = player != null ? player.Library : null;
            if (library == null || library.Find("FxOrbiter") == null)
            {
                enabled = false;
                return false;
            }

            var found = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Orbiter") && child.GetComponent<SpriteRenderer>() != null)
                {
                    found.Add(child);
                }
            }

            if (found.Count == 0)
            {
                return false; // the field builds them in its Start; try again next frame
            }

            _orbiters = found.ToArray();
            _last = new Vector3[_orbiters.Length];
            for (int i = 0; i < _orbiters.Length; i++)
            {
                Transform orbiter = _orbiters[i];
                orbiter.GetComponent<SpriteRenderer>().color = Color.white;

                // The art goes on a child so the orbiter's own scale (its hit size) is left alone.
                var art = new GameObject("Ant", typeof(SpriteRenderer));
                art.transform.SetParent(orbiter, false);
                art.transform.localScale = Vector3.one * (AntScale / Mathf.Max(0.01f, orbiter.localScale.x));
                art.GetComponent<SpriteRenderer>().sortingOrder = orbiter.GetComponent<SpriteRenderer>().sortingOrder;
                orbiter.GetComponent<SpriteRenderer>().enabled = false;
                art.AddComponent<SpriteClipPlayer>().Configure(library, "FxOrbiter", "Fly");
                _last[i] = orbiter.position;
            }

            return true;
        }
    }
}
