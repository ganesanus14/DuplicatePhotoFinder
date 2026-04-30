using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateEngine.Face
{
    public class FaceGrouper
    {
        public List<FaceGroup> GroupFaces(List<FaceInfo> faces, double threshold = 0.90)
        {
            var groups = new List<FaceGroup>();
            int groupId = 1;

            var used = new HashSet<FaceInfo>();

            foreach (var face in faces)
            {
                if (used.Contains(face)) continue;

                var group = new FaceGroup { GroupId = groupId++ };
                group.Faces.Add(face);
                used.Add(face);

                foreach (var other in faces)
                {
                    if (used.Contains(other)) continue;

                    double sim = Cosine(face.Embedding, other.Embedding);
                    if (sim >= threshold)
                    {
                        group.Faces.Add(other);
                        used.Add(other);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        private double Cosine(float[] a, float[] b)
        {
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }
    }

}
