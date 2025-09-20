using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using NUnit;

public class Chief : Agent
{
    public enum HeldItem
    {
        None,
        Onion,
        Dish,
        SoupReady
    }

    public HeldItem heldItem = HeldItem.None;
    public CookingEnvController envController;

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
        //transform.localPosition = new Vector3(Random.Range(-3f, 3f), 0.5f, Random.Range(-2f, 2f));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(envController.onion.transform.position - transform.localPosition);
        sensor.AddObservation(envController.dish.transform.position - transform.localPosition);
        sensor.AddObservation(envController.pot.transform.position - transform.localPosition);
        sensor.AddObservation(envController.counter.transform.position - transform.localPosition);
        
        sensor.AddObservation((float)heldItem);

        sensor.AddObservation(envController.hasOnion ? 1f : 0f);
        sensor.AddObservation(envController.hasDish ? 1f : 0f);

        sensor.AddObservation(envController.PotHasOnion ? 1f : 0f);
        sensor.AddObservation(envController.PotHasDish ? 1f : 0f);
        sensor.AddObservation(envController.PotHasSoupReady ? 1f : 0f);

        //sensor.AddObservation(envController.isCooked ? 1f : 0f);
        sensor.AddObservation(envController.isServerd ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = 2f * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = 2f * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        Vector3 move = new Vector3(moveX, 0f, moveZ) * 4f;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);

        AddReward(-1f / envController.MaxEnvSteps);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Onion") && heldItem == HeldItem.None && !envController.hasOnion)
        {
            heldItem = HeldItem.Onion;
            envController.hasOnion = true;
            AddReward(1f);
        }

        else if (collision.collider.CompareTag("Dish") && heldItem == HeldItem.None && !envController.hasDish)
        {
            heldItem = HeldItem.Dish;
            envController.hasDish = true;
            AddReward(1f);
        }

        else if (collision.collider.CompareTag("Pot"))
        {
            if (heldItem == HeldItem.Dish || heldItem == HeldItem.Onion)
            {
                if (!envController.IsItemInPot(heldItem))
                {
                    envController.PutInPot(heldItem);
                    AddReward(1f);
                    heldItem = HeldItem.None;
                }

                if (envController.CheckSoupReady())
                {
                    envController.GenerateSoup();
                    AddReward(2f);
                    envController.isCooked = true;
                }
            }

            else if (heldItem == HeldItem.None && envController.PotHasSoupReady)
            {
                // Retrieve the soup from the pot
                heldItem = HeldItem.SoupReady;
                envController.PotHasSoupReady = false;
            }
        }

        else if (collision.collider.CompareTag("Counter") && envController.isCooked)
        {
            Debug.Log($"{gameObject.name} 撞到了柜台，heldItem={heldItem}, isCooked={envController.isCooked}, isServerd={envController.isServerd}");

            if (heldItem == HeldItem.SoupReady && !envController.isServerd)
            {
                envController.ServeSuccess();
                heldItem = HeldItem.None;
                envController.agentRenderer.material.color = Color.cyan;
            }
        }

        else if (collision.collider.CompareTag("wall"))
        {
            AddReward(-0.0001f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> action = actionsOut.ContinuousActions;
        action[0] = Input.GetAxisRaw("Horizontal");
        action[1] = Input.GetAxisRaw("Vertical");
    }

    public void ResetAgent()
    {
        heldItem = HeldItem.None;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}