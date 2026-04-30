namespace DuplicateEngine.Face
{
    public class FaceGroup
    {
        public int GroupId { get; set; }
        public List<FaceInfo> Faces { get; set; } = new();
    }

}
