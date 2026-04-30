using OpenCvSharp;

namespace DuplicateEngine.Face
{
    public class FaceEngine
    {
        private readonly CascadeClassifier _faceCascade;

        public FaceEngine()
        {
            _faceCascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
        }

        public List<FaceInfo> DetectFaces(string imagePath)
        {
            using var img = Cv2.ImRead(imagePath);
            var gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            var faces = _faceCascade.DetectMultiScale(gray, 1.1, 4);

            var results = new List<FaceInfo>();

            foreach (var face in faces)
            {
                var faceMat = new Mat(img, face);
                var embedding = ComputeSimpleEmbedding(faceMat);

                results.Add(new FaceInfo
                {
                    ImagePath = imagePath,
                    Bounds = face,
                    Embedding = embedding
                });
            }

            return results;
        }

        private float[] ComputeSimpleEmbedding(Mat face)
        {
            // Resize to 32x32 for a tiny embedding
            var resized = face.Resize(new Size(32, 32));

            var embedding = new float[32 * 32 * 3];
            int idx = 0;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    var color = resized.Get<Vec3b>(y, x);
                    embedding[idx++] = color.Item0 / 255f;
                    embedding[idx++] = color.Item1 / 255f;
                    embedding[idx++] = color.Item2 / 255f;
                }
            }

            return embedding;
        }
    }

}
