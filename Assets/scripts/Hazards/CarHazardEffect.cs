using System.Collections;
using UnityEngine;

public class CarHazardEffect : MonoBehaviour
{
    private Rigidbody body;

    private Coroutine activeEffect;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Apply(RoadHazard.HazardType type)
    {
        if (activeEffect != null)
            StopCoroutine(activeEffect);

        switch (type)
        {
            case RoadHazard.HazardType.Oil:
                activeEffect = StartCoroutine(OilEffect());
                break;

            case RoadHazard.HazardType.Mud:
                activeEffect = StartCoroutine(MudEffect());
                break;

            case RoadHazard.HazardType.Rough:
                activeEffect = StartCoroutine(RoughEffect());
                break;
        }
    }

    private IEnumerator OilEffect()
    {
        float duration = 1.6f;

        float timer = 0f;

        float direction =
            Random.value > 0.5f ? 1f : -1f;

        while (timer < duration)
        {
            body.AddForce(
                transform.right *
                direction *
                3.5f,
                ForceMode.Acceleration
            );

            body.AddTorque(
                Vector3.up *
                direction *
                1.5f,
                ForceMode.Acceleration
            );

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        activeEffect = null;
    }

    private IEnumerator MudEffect()
    {
        float duration = 2f;

        float timer = 0f;

        while (timer < duration)
        {
            body.linearVelocity *= 0.96f;

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        activeEffect = null;
    }

    private IEnumerator RoughEffect()
    {
        body.linearVelocity *= 0.72f;

        body.AddForce(
            Vector3.up * 1.5f,
            ForceMode.VelocityChange
        );

        yield return new WaitForSeconds(0.4f);

        activeEffect = null;
    }
}