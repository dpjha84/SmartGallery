using ImageClassification.ImageDataStructures;
using ImageClassification.ModelScorer;
using System;
using System.Collections.Generic;
using System.IO;


namespace ImageClassification
{
    public class Predictor
    {
        static void Main(string[] args)
        {
            Predict();
        }
        public static IEnumerable<ImageNetDataProbability> Predict()
        {
            string assetsRelativePath = @"C:\Hack\Repos\SmartGallery\ImageClassification\assets";
            string assetsPath = GetAbsolutePath(assetsRelativePath);

            var tagsTsv = Path.Combine(assetsPath, "inputs", "images", "tags.tsv");
            var imagesFolder = @"C:\Hack\DeepLearning_ImageClassification_Training\ImageClassification.Train\assets\inputs\images\flower_photos_small_set\flower_photos_small_set";// Path.Combine(assetsPath, "inputs", "images");
            var inceptionPb = Path.Combine(assetsPath, "inputs", "inception", "tensorflow_inception_graph.pb");
            var labelsTxt = Path.Combine(assetsPath, "inputs", "inception", "imagenet_comp_graph_label_strings.txt");

            try
            {
                var modelScorer = new TFModelScorer(tagsTsv, imagesFolder, inceptionPb, labelsTxt);
                var scores = modelScorer.Score();
                return scores;

            }
            catch (Exception ex)
            {
                return null;
                //ConsoleHelpers.ConsoleWriteException(ex.ToString());
            }

            //ConsoleHelpers.ConsolePressAnyKey();
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Predictor).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;
            string fullPath = Path.Combine(assemblyFolderPath, relativePath);
            return fullPath;
        }
    }
}
