using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank
{
    public GameObject obj;
    public TankNN nn;
    public GameObject target;
    public float score = 0f;

    public Tank(int[] layerSizes)
    {
        nn = new TankNN(layerSizes);
    }

    public Tank(Tank parent, float mutationStrength)
    {
        nn = parent.nn;
        nn.Mutate(mutationStrength);
    }
}

public class TankNN
{
    Layer[] layers;

    public TankNN(int[] layerSizes)
    {
        layers = new Layer[layerSizes.Length - 1];
        for (int i = 1; i < layerSizes.Length; i++)
        {
            layers[i - 1] = new Layer(layerSizes[i - 1], layerSizes[i]);
        }
    }

    public float[] FeedForward(float[] inputs)
    {
        float[] outputs = inputs;
        for (int i = 0; i < layers.Length; i++)
        {
            outputs = layers[i].FeedForward(outputs);
        }
        return outputs;
    }

    public void Mutate(float mutationStrength)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].Mutate(mutationStrength);
        }
    }
}

public class Layer
{
    public float[] weights;
    public float[] biases;

    public Layer(int prevNumNodes, int numNodes)
    {
        weights = new float[prevNumNodes * numNodes];
        biases = new float[numNodes];
        InitializeWithRandom();
    }

    public float[] FeedForward(float[] inputs)
    {
        float[] outputs = new float[biases.Length];
        for (int i = 0; i < biases.Length; i++)
        {
            outputs[i] = biases[i];
            for (int j = 0; j < inputs.Length; j++)
            {
                outputs[i] += inputs[j] * weights[i * inputs.Length + j];
            }
            outputs[i] = ActivationFunction(outputs[i]);
        }
        return outputs;
    }

    public void Mutate(float mutationStrength)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] += (Random.value * 2f - 1f) * mutationStrength;
        }
        for (int i = 0; i < biases.Length; i++)
        {
            biases[i] += (Random.value * 2f - 1f) * mutationStrength;
        }
    }

    private float ActivationFunction(float value)
    {
        return Mathf.Max(0f, value);
    }

    private void InitializeWithRandom()
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Random.value * 2f - 1f;
        }
        for (int i = 0; i < biases.Length; i++)
        {
            biases[i] = Random.value * 2f - 1f;
        }
    }

    private void InitializeWithZero()
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = 0f;
        }
        for (int i = 0; i < biases.Length; i++)
        {
            biases[i] = 0f;
        }
    }
}