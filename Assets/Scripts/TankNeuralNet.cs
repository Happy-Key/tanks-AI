using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tank
{
    public GameObject obj;
    public TankNN nn;
    public GameObject target;
    public float score = 0f;
    public Color color = Random.ColorHSV();

    public Tank(int[] layerSizes)
    {
        nn = new TankNN(layerSizes);
    }

    public Tank(Tank parent, float mutationStrength)
    {
        nn = new TankNN(parent.nn);
        nn.Mutate(mutationStrength);
        color = Color.Lerp(parent.color, Random.ColorHSV(), mutationStrength);
    }
}

[System.Serializable]
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

    public TankNN(TankNN parent)
    {
        layers = new Layer[parent.layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new Layer(parent.layers[i]);
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
        // Mutate all parameters at once
        ///*
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].Mutate(mutationStrength);
        }
        //*/

        // Mutate only a group of parameters
        /*
        layers[Random.Range(0, layers.Length)].Mutate(mutationStrength);
        */
    }
}

[System.Serializable]
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

    public Layer(Layer parent)
    {
        weights = new float[parent.weights.Length];
        biases = new float[parent.biases.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = parent.weights[i];
        }
        for (int i = 0; i < biases.Length; i++)
        {
            biases[i] = parent.biases[i];
        }
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
        // Mutate all parameters at once
        ///*
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] += Utils.RandomGaussian(-1f, 1f) * mutationStrength;
        }
        for (int i = 0; i < biases.Length; i++)
        {
            biases[i] += Utils.RandomGaussian(-1f, 1f) * mutationStrength;
        }
        //*/
        //Mutate single neuron's inputs
        /*
        int rand = Random.Range(0, biases.Length);
        biases[rand] += Utils.RandomGaussian(-1f, 1f) * mutationStrength;
        for (int i = 0; i < weights.Length / biases.Length; i++)
        {
            weights[rand * weights.Length / biases.Length + i] += Utils.RandomGaussian(-1f, 1f) * mutationStrength;
        }
        */
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

public class Utils {
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }
}