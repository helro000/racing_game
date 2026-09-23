using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RelayRacer : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    public RelayRace Race { get; private set; }

    public int Next { get; private set; } = 1;

    public bool Finished;
    public float FinishTime;
    public int BoostsUsed;

    public int Recoveries { get; private set; }

    public bool Autopilot;

    public string DisplayName =>
        name.Contains("1") ? "NOVA" : "ECHO";

    public float Fraction =>
        Mathf.Clamp01(
            (Next - 1 + SegmentFraction()) /
            (Race.Route.Count - 1f)
        );

    private inputManager input;

    private float stuck;
    private float recoverAt;
    private float aiBuildAt;

    private float lane;


    public void Setup(RelayRace race)
    {
        Race = race;

        Body = GetComponent<Rigidbody>();
        input = GetComponent<inputManager>();

        Body.interpolation =
            RigidbodyInterpolation.Interpolate;

        Body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Body.centerOfMass =
            new Vector3(0, -.35f, 0);

        Body.maxAngularVelocity = 2.5f;

        lane = CompareTag("AI")
            ? (name.Contains("1") ? -2.1f : 2.1f)
            : 0;


        // Rivals begin on visible grid slots ahead of the player.
        for (
            int i = 1;
            i < Mathf.Min(10, Race.Route.Count);
            i++
        )
        {
            if (
                HorizontalDistance(
                    transform.position,
                    Race.Route[i].position
                )
                <
                HorizontalDistance(
                    transform.position,
                    Race.Route[Next - 1].position
                )
            )
            {
                Next = i + 1;
            }
        }


        aiBuildAt =
            name.Contains("1") ? 14 : 21;
    }


    float SegmentFraction()
    {
        if (
            Race == null ||
            Race.Route == null
        )
        {
            return 0;
        }


        var a =
            Race.Route[
                Mathf.Max(0, Next - 1)
            ].position;


        var b =
            Race.Route[
                Mathf.Min(
                    Next,
                    Race.Route.Count - 1
                )
            ].position;


        return Mathf.Clamp01(
            Vector3.Dot(
                transform.position - a,
                b - a
            )
            /
            Mathf.Max(
                .01f,
                (b - a).sqrMagnitude
            )
        );
    }


    void FixedUpdate()
    {
        if (Race == null)
            return;


        input.vertical = 0;
        input.horizontal = 0;

        input.handbrake = false;
        input.boosting = false;


        if (
            !Race.Running ||
            Finished
        )
        {
            return;
        }


        // Ordered local progress:
        // proximity to a distant part of the road
        // cannot skip the course.
        while (
            Next < Race.Route.Count &&
            PassedNode()
        )
        {
            if (
                Next ==
                Race.Route.Count - 1
            )
            {
                Race.Finish(this);
                return;
            }

            Next++;
        }


        input.currentNode =
            Next - 1;


        bool ai =
            CompareTag("AI") ||
            Autopilot;


        if (ai)
        {
            DriveAI();
        }
        else
        {
            input.vertical =
                Input.GetAxis("Vertical");

            input.horizontal =
                Input.GetAxis("Horizontal");

            input.handbrake =
                Input.GetKey(KeyCode.Space);
        }


        if (
            Body.linearVelocity.magnitude < 1.2f ||
            Vector3.Dot(
                transform.up,
                Vector3.up
            ) < .35f
        )
        {
            stuck +=
                Time.fixedDeltaTime;
        }
        else
        {
            stuck = 0;
        }


        if (
            (ai && stuck > 4) ||

            transform.position.y <
            Race.Route[Next].position.y - 12 ||

            (
                ai &&
                HorizontalDistance(
                    transform.position,
                    Race.Route[Next].position
                ) > 30
            )
        )
        {
            Recover();
        }


        if (
            ai &&
            Race.Elapsed >= aiBuildAt
        )
        {
            // Rivals save their strips for straights,
            // leaving braking room before bends.

            if (
                Vector3.Angle(
                    Race.Direction(Next),

                    Race.Direction(
                        Mathf.Min(
                            Next + 10,
                            Race.Route.Count - 2
                        )
                    )
                ) < 12
            )
            {
                GetComponent<RoadBuilderPower>()
                    .TryBuild();

                aiBuildAt =
                    Race.Elapsed + 18;
            }
            else
            {
                aiBuildAt =
                    Race.Elapsed + 1;
            }
        }
    }


    bool PassedNode()
    {
        var delta =
            transform.position -
            Race.Route[Next].position;

        delta.y = 0;


        var direction =
            Race.Direction(
                Mathf.Max(
                    0,
                    Next - 1
                )
            );


        float along =
            Vector3.Dot(
                delta,
                direction
            );


        float lateral =
            Mathf.Abs(
                Vector3.Dot(
                    delta,

                    Vector3.Cross(
                        Vector3.up,
                        direction
                    )
                )
            );


        // Crossing a full road-width plane counts,
        // but teleporting to a distant segment does not.

        return
            along >= 0 &&
            along < 14 &&
            lateral < 7.4f;
    }


    void DriveAI()
    {
        float speed =
            Body.linearVelocity.magnitude;


        int aim =
            Next;


        float ahead =
            Mathf.Clamp(
                7 + speed * .5f,
                8,
                20
            );


        while (
            aim < Race.Route.Count - 1 &&

            HorizontalDistance(
                transform.position,
                Race.Route[aim].position
            ) < ahead
        )
        {
            aim++;
        }


        // ------------------------------
        // OVERTAKING / COLLISION AVOIDANCE
        // ------------------------------

        float desiredLane =
            lane;


        foreach (
            var other in Race.Racers
        )
        {
            if (
                other == this ||
                other.Finished
            )
            {
                continue;
            }


            var local =
                transform.InverseTransformPoint(
                    other.transform.position
                );


            if (
                local.z > 0 &&
                local.z < 13 &&
                Mathf.Abs(local.x) < 2.5f
            )
            {
                desiredLane =
                    lane <= 0
                    ? 2.1f
                    : -2.1f;
            }
        }


        var target =
            Race.Route[aim].position +

            Vector3.Cross(
                Vector3.up,
                Race.Direction(aim)
            ) * desiredLane;


        var relative =
            transform.InverseTransformPoint(
                target
            );


        float angle =
            Mathf.Atan2(
                relative.x,
                relative.z
            ) * Mathf.Rad2Deg;


        float steering =
            Mathf.Atan2(
                2f *
                2.55f *
                relative.x,

                relative.x *
                relative.x +

                relative.z *
                relative.z
            ) * Mathf.Rad2Deg;


        input.horizontal =
            Mathf.Clamp(
                steering /
                GetComponent<controller>()
                    .RelaySteerAngle,

                -1,
                1
            );


        // ------------------------------
        // CORNER DETECTION
        // ------------------------------

        float bend =
            Vector3.Angle(
                Race.Direction(Next),

                Race.Direction(
                    Mathf.Min(
                        aim + 7,
                        Race.Route.Count - 2
                    )
                )
            );


        float cornerAmount =
            Mathf.Clamp01(
                Mathf.Max(
                    Mathf.Abs(angle),
                    bend
                ) / 65f
            );


        // ------------------------------
        // DIFFICULTY SPEED
        // 0 = EASY
        // 1 = NORMAL
        // 2 = HARD
        //
        // Speeds are metres per second.
        // ------------------------------

        float straightSpeed;
        float cornerSpeed;


        if (
            RelaySettings.Difficulty == 0
        )
        {
            // EASY
            // Straight: about 94 km/h
            // Corner: about 47 km/h

            straightSpeed = 26f;
            cornerSpeed = 13f;
        }
        else if (
            RelaySettings.Difficulty == 2
        )
        {
            // HARD
            // Straight: about 130 km/h
            // Corner: about 65 km/h

            straightSpeed = 36f;
            cornerSpeed = 18f;
        }
        else
        {
            // NORMAL
            // Straight: about 108 km/h
            // Corner: about 54 km/h

            straightSpeed = 30f;
            cornerSpeed = 15f;
        }


        float targetSpeed =
            Mathf.Lerp(
                straightSpeed,
                cornerSpeed,
                cornerAmount
            );


        // ------------------------------
        // SMALL RUBBER-BANDING
        // ------------------------------

        if (
            CompareTag("AI")
        )
        {
            float gap =
                (
                    Race.Player.Fraction -
                    Fraction
                )
                *
                Race.Route.Count
                *
                3.5f;


            if (
                RelaySettings.Difficulty == 0
            )
            {
                // EASY

                targetSpeed +=
                    Mathf.Clamp(
                        gap * .025f,
                        -1.5f,
                        1.5f
                    );
            }
            else if (
                RelaySettings.Difficulty == 2
            )
            {
                // HARD

                targetSpeed +=
                    Mathf.Clamp(
                        gap * .045f,
                        -3f,
                        3f
                    );
            }
            else
            {
                // NORMAL

                targetSpeed +=
                    Mathf.Clamp(
                        gap * .035f,
                        -2f,
                        2f
                    );
            }
        }


        // ------------------------------
        // THROTTLE / BRAKING
        // ------------------------------

        float speedDifference =
            targetSpeed - speed;


        if (
            speed >
            targetSpeed + 1.5f
        )
        {
            // Brake before corners
            input.vertical =
                -.65f;
        }
        else
        {
            float throttle;


            if (
                RelaySettings.Difficulty == 0
            )
            {
                // EASY

                throttle =
                    Mathf.Clamp01(
                        speedDifference *
                        .45f +
                        .35f
                    );
            }
            else if (
                RelaySettings.Difficulty == 2
            )
            {
                // HARD

                throttle =
                    Mathf.Clamp01(
                        speedDifference *
                        .8f +
                        .55f
                    );
            }
            else
            {
                // NORMAL

                throttle =
                    Mathf.Clamp01(
                        speedDifference *
                        .6f +
                        .45f
                    );
            }


            input.vertical =
                throttle;
        }
    }


    public void Recover()
    {
        if (
            !Race.Running ||
            Finished ||
            Time.time < recoverAt
        )
        {
            return;
        }


        int i =
            Mathf.Max(
                0,
                Next - 3
            );


        Body.linearVelocity =
            Vector3.zero;

        Body.angularVelocity =
            Vector3.zero;


        Body.position =
            Race.Route[i].position +

            Vector3.Cross(
                Vector3.up,
                Race.Direction(i)
            ) * lane +

            Vector3.up * .6f;


        Body.rotation =
            Quaternion.LookRotation(
                Race.Direction(i)
            );


        Next =
            Mathf.Max(
                1,
                i + 1
            );


        stuck = 0;

        Recoveries++;


        Debug.Log(
            "RELAY_RECOVERY " +
            name +
            " node=" +
            Next +
            " position=" +
            transform.position
        );


        recoverAt =
            Time.time + 2;


        foreach (
            var trail in
            GetComponentsInChildren<TrailRenderer>()
        )
        {
            trail.Clear();
        }


        if (
            CompareTag("Player")
        )
        {
            Race.Notify(
                "Back on track"
            );
        }
    }


    public static float HorizontalDistance(
        Vector3 a,
        Vector3 b
    )
    {
        a.y = 0;
        b.y = 0;

        return Vector3.Distance(
            a,
            b
        );
    }
}