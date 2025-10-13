using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using NUnit;
using UnityEngine.Rendering;

public class FCChief : Agent
{
    public enum HeldItem
    {
        None,
        Onion,
        Dish,
        Soup
    }

    public HeldItem heldItem = HeldItem.None;
    public FCCookingEnvController envController;
    public Transform Plane;

    public Circuit[] circuit;

    private float bounaryX;
    private float bounaryZ;

    private Rigidbody rb;
   //public Renderer agentRenderer;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
        envController.ResetEnv();
    }

    void Start()
    {
        bounaryX = Plane.localScale.x * 5f;
        bounaryZ = Plane.localScale.z * 5f;  
    }

    void FixedUpdate()
    {
        Vector3 localPos = transform.localPosition - Plane.localPosition;
        
        if (Mathf.Abs(localPos.x) > bounaryX || Mathf.Abs(localPos.z) > bounaryZ || transform.localPosition.y < -2f) // out of bounds
        {
            if (transform.name == "Chief1")
            {
                transform.localPosition = new Vector3(3.33f, 0.5f, Random.Range(3.42f, -0.15f));
            }
            else if (transform.name == "Chief2")
            {
                transform.localPosition = new Vector3(-0.76f, 0.5f, Random.Range(3.28f, -0.33f));
            }
        }
    }
    // state coverage for continuous state space
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);

        sensor.AddObservation(envController.onion.transform.position);
        sensor.AddObservation(envController.onion1.transform.position);
        sensor.AddObservation(envController.dish.transform.position);
        sensor.AddObservation(envController.table.transform.position);
        sensor.AddObservation(envController.pot.transform.position);
        sensor.AddObservation(envController.pot1.transform.position);
        sensor.AddObservation(envController.counter.transform.position);

        sensor.AddObservation((float)heldItem);

        sensor.AddObservation(envController.PotHasSoupReady);
        sensor.AddObservation(envController.OnionCount);
        sensor.AddObservation(envController.RetriveSoup);

        sensor.AddObservation(envController.isServerd? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //DiscreteMovement(actions.DiscreteActions);
        ContinuousMovement(actions);
        AddReward(-1f / envController.MaxEnvSteps);
    }

    // continuous action movement
    public void ContinuousMovement(ActionBuffers actions)
    {
        float moveX = 2f * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = 2f * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector3 move = new Vector3(moveX, 0f, moveZ) * 4f;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
    }

    // discrete action movement
    public void DiscreteMovement(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        rb.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Onion") && heldItem == HeldItem.None && transform.name == "Chief1")
        {
            heldItem = HeldItem.Onion;
            AddReward(1f);
        }

        if (collision.collider.CompareTag("Dish") && heldItem == HeldItem.None && transform.name == "Chief1")
        {
            heldItem = HeldItem.Dish;
            AddReward(1f);
        }

        // if (collision.collider.CompareTag("Table"))
        // {
        //     // put item on table
        //     if ((heldItem == HeldItem.Dish || heldItem == HeldItem.Onion) && transform.name == "Chief1")
        //     {
        //         envController.PutInTable(heldItem);
        //         heldItem = HeldItem.None;
        //         AddReward(0.5f);
        //     }

        //     // pick item from table
        //     if (heldItem == HeldItem.None && envController.TableHasOnion && !envController.PotHasSoupReady && transform.name == "Chief2")
        //     {
        //         heldItem = HeldItem.Onion;
        //         AddReward(0.5f);

        //         envController.TableOnionCount--;

        //         if (envController.TableOnionCount == 0)
        //         { envController.TableHasOnion = false; }
        //     }

        //     if (heldItem == HeldItem.None && envController.TableHasDish && envController.PotHasSoupReady && transform.name == "Chief2")
        //     {
        //         heldItem = HeldItem.Dish;
        //         AddReward(0.5f);
        //         Debug.Log("Picked up dish from table.");
        //         envController.TableDishCount = 0;
        //         envController.TableHasDish = false;
        //     }
        // }

        if (collision.collider.CompareTag("Circuit") && transform.name == "Chief1")
        {   var c = collision.collider.GetComponent<Circuit>();
            if (c!=null && !c.Occupied() && (heldItem == HeldItem.Dish || heldItem == HeldItem.Onion))
            {
                c.PlaceItem(heldItem);
                heldItem = HeldItem.None;
                AddReward(0.2f);
            }
        }
        if(collision.collider.CompareTag("Circuit") && transform.name == "Chief2")
        {
            var c = collision.collider.GetComponent<Circuit>();
            if (c!=null && c.Occupied() && heldItem == HeldItem.None)
            {
                heldItem = c.TakeItem();
                AddReward(0.2f);
                Debug.Log($"Picked up {heldItem} from circuit.");
            }
        }
        
        if (collision.collider.CompareTag("Pot") && transform.name == "Chief2")
        {
            if (heldItem == HeldItem.Onion && !envController.PotHasSoupReady && envController.OnionCount != 3)
            {
                Debug.Log("OnionCount: " + envController.OnionCount);
                envController.PutInPot(heldItem);
                heldItem = HeldItem.None;
                AddReward(0.2f);

                if (envController.CheckSoupReady())
                {
                    envController.GenerateSoup();
                }
            }

            // retrieve soup from pot
            else if (heldItem == HeldItem.Dish)
            {
                if (envController.PotHasSoupReady)
                {
                    heldItem = HeldItem.Soup;
                    envController.PotHasSoupReady = false;
                    envController.OnionCount = 0;

                    envController.RetriveSoup = true;

                    AddReward(1f);
                    Debug.Log("Picked up Soup from pot.");
                }
                else
                {
                    Debug.Log("Pot does not have ready soup, you should cook soup first");
                    AddReward(-0.5f);
                    heldItem = HeldItem.None;
                }
            }
        }
            
        if(collision.collider.CompareTag("Counter") && transform.name == "Chief2")
            {
                if(heldItem == HeldItem.Soup && !envController.isServerd)
                {
                    heldItem = HeldItem.None;
                    envController.ServeSuccess();
                }
            }

        if (collision.collider.CompareTag("wall"))
        {
            AddReward(-0.0001f);
        }
    }

    // continuoues action for testing with keyboard
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> action = actionsOut.ContinuousActions;
        action[0] = Input.GetAxisRaw("Horizontal");
        action[1] = Input.GetAxisRaw("Vertical");
    }

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     var discreteActionsOut = actionsOut.DiscreteActions;
    //     if (Input.GetKey(KeyCode.D))
    //     {
    //         discreteActionsOut[0] = 3;
    //     }
    //     else if (Input.GetKey(KeyCode.W))
    //     {
    //         discreteActionsOut[0] = 1;
    //     }
    //     else if (Input.GetKey(KeyCode.A))
    //     {
    //         discreteActionsOut[0] = 4;
    //     }
    //     else if (Input.GetKey(KeyCode.S))
    //     {
    //         discreteActionsOut[0] = 2;
    //     }
    // }

    public void ResetAgent()
    {
        heldItem = HeldItem.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
