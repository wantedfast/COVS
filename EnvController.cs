using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class CookingEnvController : MonoBehaviour
{
    [Header("Agents & Objects")]
    public List<Chief> agentList = new List<Chief>();
    public GameObject onion;
    public GameObject dish;
    public GameObject pot;
    public GameObject counter;

    public Renderer agentRenderer;


    [Header("State Flags")]
    public bool hasOnion = false;
    public bool hasDish = false;
    public bool PotHasOnion = false;
    public bool PotHasDish = false;
    public bool isCooked = false;

    public bool isServerd = false;

    [Header("Step Settings")]
    public int MaxEnvSteps = 15000;
    private int stepCount = 0;

    [Header("Group Reward Manager")]
    public SimpleMultiAgentGroup M_agentGroup;

    [Header("Last Agent Record")]
    public string lastAgentToPut = "";

    void Start()
    {
        M_agentGroup = new SimpleMultiAgentGroup();

        foreach (var agent in agentList)
        {
            M_agentGroup.RegisterAgent(agent);
        }

        ResetEnv();
    }

    void Awake()
    {
        MaxEnvSteps = 15000;
    }

    void FixedUpdate()
    {
        stepCount++;

        if (stepCount >= MaxEnvSteps)
        {
            Debug.Log("🔚 Reached max step without success");
            M_agentGroup.GroupEpisodeInterrupted();
            ResetEnv();
        }
    }

    public bool PutInPot(Chief.HeldItem item)
    {
        if (item == Chief.HeldItem.Onion && !PotHasOnion)
        {
            PotHasOnion = true;
        }
        else if (item == Chief.HeldItem.Dish && !PotHasDish)
        {
            PotHasDish = true;
        }

        if (PotHasOnion && PotHasDish && !isCooked)
        {
            isCooked = true;
            float timeReward = 1f - (float)stepCount / MaxEnvSteps;
            M_agentGroup.AddGroupReward(2f);
            agentRenderer.material.color = Color.green; // Change color to indicate cooking success

            return true;
        }

        return false;
    }

    public bool IsItemInPot(Chief.HeldItem item)
    {
        if (item == Chief.HeldItem.Onion)
            return PotHasOnion;
        else if (item == Chief.HeldItem.Dish)
            return PotHasDish;
        return false;
    }

    public void ServeSuccess()
    {
        float timeReward = 1f - (float)stepCount / MaxEnvSteps;
        M_agentGroup.AddGroupReward(3f); // 上菜成功奖励
        isServerd = true;
        Debug.Log("🍽️ Served successfully!");
        M_agentGroup.EndGroupEpisode();
        ResetEnv();
    }

    public void WallHitted()
    {
        // 只有锅完全空时才惩罚撞墙
        if (!PotHasOnion && !PotHasDish)
        {
            Debug.Log("💥撞墙惩罚，锅空");
            M_agentGroup.AddGroupReward(-1f);
            M_agentGroup.EndGroupEpisode();
            ResetEnv();
        }
        else
        {
            Debug.Log("🚧 撞墙但锅已有物品，不惩罚");
        }
    }

    public void ResetEnv()
    {
        stepCount = 0;
        hasOnion = false;
        hasDish = false;
        PotHasOnion = false;
        PotHasDish = false;
        isCooked = false;
        isServerd = false;
        lastAgentToPut = "";
        // 重置 agent 位置（避免出生在桌子上可进一步优化）
        foreach (var agent in agentList)
        {
            agent.transform.localPosition = new Vector3(Random.Range(-3f, 3f), 0.5f, Random.Range(-2f, 2f));
        }
    }
}
