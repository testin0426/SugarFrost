using System.Collections;
using System.Collections.Generic;
using Naninovel;
using Naninovel.Commands;
using System;
using UnityEngine;

[Alias("CalculateExam")]
public class CalculateExam : Command
{
    public DecimalParameter mSum;
    public DecimalParameter count;

    public override Awaitable Execute (ExecutionContext ctx)
    {
        var varibleManager = Engine.GetService<ICustomVariableManager>();
        float? nullableM = mSum;
        float? nullableC = count;
        float mValue = nullableM.HasValue ? nullableM.Value : 0.5f;
        float cValue = nullableC.HasValue ? nullableC.Value : 2f;
        float avgM = mValue / cValue;

        float mu = 40f + 60f * avgM;
        float sigma = 25f * (1.1f - avgM) + 5f;

        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        float score = mu + sigma * randStdNormal;
        score = Mathf.Clamp(score, 0, 100);

        int rank = Mathf.FloorToInt(1001f - Mathf.Pow(score / 100f, 3f) * 1000f);
        rank = Mathf.Clamp(rank, 1, 1000);

        varibleManager.SetVariableValue("LastG", new CustomVariableValue(rank));
        if(varibleManager.TryGetVariableValue<float>("MaxM", out var maxVal)){
            if (rank > 300)
            {   
                float currentMaxM = 1.0f;
                currentMaxM = maxVal;
                float penalty = (rank - 300) / 900f * 0.6f;
                float newMaxM = Mathf.Max(0.1f, 1 - penalty);
                varibleManager.SetVariableValue("MaxM", new CustomVariableValue(newMaxM));
                Debug.Log($"[Exam System] Rank: {rank}, Penalty: {penalty}, NewMaxM: {newMaxM}");
            }    
        }
        return Async.Completed;
    }// Start is called before the first frame update
    
}
