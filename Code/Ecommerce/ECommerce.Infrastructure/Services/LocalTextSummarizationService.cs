using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services
{
    public class LocalTextSummarizationService : ITextSummarizationService, IDisposable
    {
        private readonly InferenceSession _encoderSession;
        private readonly InferenceSession _decoderSession;

        public LocalTextSummarizationService(string modelDirectory)
        {
            var encoderPath = Path.Combine(modelDirectory, "encoder_model.onnx");
            var decoderPath = Path.Combine(modelDirectory, "decoder_model.onnx");
            
            _encoderSession = new InferenceSession(encoderPath);
            _decoderSession = new InferenceSession(decoderPath);
        }

        public async Task<string> SummarizeTextAsync(string text)
        {
            // Create input tensor for encoder
            var inputTensor = new DenseTensor<float>(new[] { 1, text.Length });
            for (int i = 0; i < text.Length; i++)
            {
                inputTensor[0, i] = text[i];
            }

            // Run encoder
            var encoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
            };

            using var encoderResults = await Task.Run(() => _encoderSession.Run(encoderInputs));
            var encoderOutput = encoderResults.First();

            // Run decoder with encoder output
            var decoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("encoder_outputs", encoderOutput.AsTensor<float>())
            };

            using var decoderResults = await Task.Run(() => _decoderSession.Run(decoderInputs));
            var output = decoderResults.First().AsTensor<float>();

            // Convert output tensor to text (simplified for now)
            return ProcessOutput(output);
        }

        private string ProcessOutput(Tensor<float> output)
        {
            // This is a simplified implementation
            // You'll need to implement proper tokenization and detokenization based on your model
            var result = new List<char>();
            for (int i = 0; i < output.Dimensions[1]; i++)
            {
                result.Add((char)Math.Round(output[0, i]));
            }
            return new string(result.ToArray());
        }

        public void Dispose()
        {
            _encoderSession?.Dispose();
            _decoderSession?.Dispose();
        }
    }
}