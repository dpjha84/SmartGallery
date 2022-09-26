using Common;
using ImageClassification.DataModels;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Microsoft.ML.Transforms.ValueToKeyMappingEstimator;

namespace ImageClassification.Train
{
    public class CustomModelPhotoRecognition
    {
        private static string outputMlNetModelFilePath;
        private static string imagesFolderPathForPredictions;
        private static MLContext mlContext;
        private static ITransformer trainedModel;

        public static void TrainAndSaveModel(Dictionary<string, List<string>> labelAndPaths)
        {
            const string assetsRelativePath = @"../../../assets";
            string assetsPath = GetAbsolutePath(assetsRelativePath);
            if (!Directory.Exists(Path.Combine(assetsPath, "outputs")))
            {
                Directory.CreateDirectory(Path.Combine(assetsPath, "outputs"));
            }
            outputMlNetModelFilePath = Path.Combine(assetsPath, "outputs", "imageClassifier.zip");
            string imagesDownloadFolderPath = Path.Combine(assetsPath, "inputs", "images");

            if (!Directory.Exists(imagesDownloadFolderPath))
            {
                Directory.CreateDirectory(imagesDownloadFolderPath);
            }

            string finalImagesFolderName = CopyFolder(labelAndPaths, Path.Combine(imagesDownloadFolderPath, "Photos"));
            string fullImagesetFolderPath = Path.Combine(imagesDownloadFolderPath, finalImagesFolderName);

            mlContext = new MLContext(seed: 1);
            mlContext.Log += FilterMLContextLog;

            // 2. Load the initial full image-set into an IDataView and shuffle so it'll be better balanced
            IEnumerable<ImageData> images = LoadImagesFromDirectory(folder: fullImagesetFolderPath, useFolderNameAsLabel: true);

            IDataView fullImagesDataset = mlContext.Data.LoadFromEnumerable(images);
            IDataView shuffledFullImageFilePathsDataset = mlContext.Data.ShuffleRows(fullImagesDataset);

            // 3. Load Images with in-memory type within the IDataView and Transform Labels to Keys (Categorical)
            IDataView shuffledFullImagesDataset = mlContext.Transforms.Conversion.
                    MapValueToKey(outputColumnName: "LabelAsKey", inputColumnName: "Label", keyOrdinality: KeyOrdinality.ByValue)
                .Append(mlContext.Transforms.LoadRawImageBytes(
                                                outputColumnName: "Image",
                                                imageFolder: fullImagesetFolderPath,
                                                inputColumnName: "ImagePath"))
                .Fit(shuffledFullImageFilePathsDataset)
                .Transform(shuffledFullImageFilePathsDataset);

            // 4. Split the data 80:20 into train and test sets, train and evaluate.
            var trainTestData = mlContext.Data.TrainTestSplit(shuffledFullImagesDataset, testFraction: 0.2);
            IDataView trainDataView = trainTestData.TrainSet;
            IDataView testDataView = trainTestData.TestSet;

            // 5. Define the model's training pipeline using DNN default values
            //
            var pipeline = mlContext.MulticlassClassification.Trainers
                    .ImageClassification(featureColumnName: "Image",
                                         labelColumnName: "LabelAsKey",
                                         validationSet: testDataView)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel",
                                                                      inputColumnName: "PredictedLabel"));

            // 6. Train/create the ML model
            Console.WriteLine("*** Training the image classification model with DNN Transfer Learning on top of the selected pre-trained model/architecture ***");

            // Measuring training time
            var watch = Stopwatch.StartNew();

            //Train
            trainedModel = pipeline.Fit(trainDataView);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine($"Training with transfer learning took: {elapsedMs / 1000} seconds");

            // 7. Get the quality metrics (accuracy, etc.)
            EvaluateModel(mlContext, testDataView, trainedModel);

            // 8. Save the model to assets/outputs (You get ML.NET .zip model file and TensorFlow .pb model file)
            mlContext.Model.Save(trainedModel, trainDataView.Schema, outputMlNetModelFilePath);
            Console.WriteLine($"Model saved to: {outputMlNetModelFilePath}");
        }

        private static void EvaluateModel(MLContext mlContext, IDataView testDataset, ITransformer trainedModel)
        {
            Console.WriteLine("Making predictions in bulk for evaluating model's quality...");

            // Measuring time
            var watch = Stopwatch.StartNew();

            var predictionsDataView = trainedModel.Transform(testDataset);

            var metrics = mlContext.MulticlassClassification.Evaluate(predictionsDataView, labelColumnName: "LabelAsKey", predictedLabelColumnName: "PredictedLabel");
            ConsoleHelper.PrintMultiClassClassificationMetrics("TensorFlow DNN Transfer Learning", metrics);

            watch.Stop();
            var elapsed2Ms = watch.ElapsedMilliseconds;

            Console.WriteLine($"Predicting and Evaluation took: {elapsed2Ms / 1000} seconds");
        }

        public static ImageData[] GetPredictionsForImagesCustomModelSaved(string testImageFolderPath)
        {
            const string assetsRelativePath = @"../../../assets";
            string assetsPath = GetAbsolutePath(assetsRelativePath);
            outputMlNetModelFilePath = Path.Combine(assetsPath, "outputs", "imageClassifier.zip");
            if (!File.Exists(outputMlNetModelFilePath))
            {
                return null;
            }
            var imageClassifierModelZipFilePath = outputMlNetModelFilePath;
            var mlContext = new MLContext(seed: 1);
            var loadedModel = mlContext.Model.Load(imageClassifierModelZipFilePath, out var modelInputSchema);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<InMemoryImageData, ImagePrediction>(loadedModel);

            var testImages = FileUtils.LoadInMemoryImagesFromDirectory(
                testImageFolderPath, false);
            List<ImageData> foundImages = new List<ImageData>();
            foreach (var imageToPredict in testImages)
            {
                var prediction = predictionEngine.Predict(imageToPredict);
                if (prediction.PredictedLabel == "")
                {
                    foundImages.Add(new ImageData(Path.Combine(imageToPredict.ImageFilePath, imageToPredict.ImageFileName), "No Label : Train More!"));
                }
                else
                {
                    foundImages.Add(new ImageData(Path.Combine(imageToPredict.ImageFilePath, imageToPredict.ImageFileName), prediction.PredictedLabel));

                }
            }

            return foundImages.ToArray();
        }

        private static IEnumerable<ImageData> LoadImagesFromDirectory(
          string folder,
          bool useFolderNameAsLabel = true)
          => FileUtils.LoadImagesFromDirectory(folder, useFolderNameAsLabel)
              .Select(x => new ImageData(x.imagePath, x.label));

        private static string CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }

            return destFolder;
        }

        private static string CopyFolder(Dictionary<string, List<string>> ImagesAndlabels, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = null;
            foreach (var item in ImagesAndlabels)
            {
                if (!Directory.Exists(Path.Combine(destFolder, item.Key)))
                    Directory.CreateDirectory(Path.Combine(destFolder, item.Key));
                files = item.Value.ToArray();
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(Path.Combine(destFolder, item.Key), name);
                    File.Copy(file, dest, true);
                }
            }

            return destFolder;
        }

        private static string GetAbsolutePath(string relativePath)
            => FileUtils.GetAbsolutePath(typeof(CustomModelPhotoRecognition).Assembly, relativePath);

        private static void FilterMLContextLog(object sender, LoggingEventArgs e)
        {
            if (e.Message.StartsWith("[Source=ImageClassificationTrainer;"))
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
   

