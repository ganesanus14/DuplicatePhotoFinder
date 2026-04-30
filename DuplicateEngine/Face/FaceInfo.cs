using OpenCvSharp;

namespace DuplicateEngine.Face
{
    public class FaceInfo
    {
        public string ImagePath { get; set; }
        public Rect Bounds { get; set; }
        public float[] Embedding { get; set; }
    }

}
