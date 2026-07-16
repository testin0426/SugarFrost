using TMPro;
using UnityEngine;

namespace Naninovel
{
    [AddComponentMenu("Naninovel/ UI/Circular Text")]
    [RequireComponent(typeof(TextMeshPro))]
    public class CircularText : MonoBehaviour
    {
        [SerializeField] private float radius = 10;

        private TextMeshPro tmp;

        public void SetText (string text)
        {
            if (!tmp) tmp = gameObject.GetComponent<TextMeshPro>();
            tmp.text = text;
            UpdateTextCurve(tmp.textInfo);
            tmp.ForceMeshUpdate();
        }

        public void SetColor (Color color)
        {
            if (!tmp) tmp = gameObject.GetComponent<TextMeshPro>();
            tmp.color = color;
        }

        private void Awake ()
        {
            tmp = gameObject.GetComponent<TextMeshPro>();
        }

        private void OnEnable ()
        {
            tmp.OnPreRenderText += UpdateTextCurve;
        }

        private void OnDisable ()
        {
            tmp.OnPreRenderText -= UpdateTextCurve;
        }

        private void UpdateTextCurve (TMP_TextInfo textInfo)
        {
            for (var i = 0; i < textInfo.characterInfo.Length; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                var vertexIndex = textInfo.characterInfo[i].vertexIndex;
                var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                var vertices = textInfo.meshInfo[materialIndex].vertices;

                var charMidBaselinePos = new Vector3(
                    (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2,
                    textInfo.characterInfo[i].baseLine);

                vertices[vertexIndex + 0] += -charMidBaselinePos;
                vertices[vertexIndex + 1] += -charMidBaselinePos;
                vertices[vertexIndex + 2] += -charMidBaselinePos;
                vertices[vertexIndex + 3] += -charMidBaselinePos;

                var matrix = ComputeTransformationMatrix(charMidBaselinePos, textInfo, i);
                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
            }
        }

        private Matrix4x4 ComputeTransformationMatrix (Vector3 charMidBaselinePos, TMP_TextInfo textInfo, int charIdx)
        {
            var radiusForThisLine = radius + textInfo.lineInfo[textInfo.characterInfo[charIdx].lineNumber].baseline;
            var circumference = 2 * radiusForThisLine * Mathf.PI;
            var angle = ((charMidBaselinePos.x / circumference - 0.5f) * 360 + 90) * Mathf.Deg2Rad;

            var x0 = Mathf.Cos(angle);
            var y0 = Mathf.Sin(angle);
            var newMidBaselinePos = new Vector2(x0 * radiusForThisLine, -y0 * radiusForThisLine);
            var rotationAngle = -Mathf.Atan2(y0, x0) * Mathf.Rad2Deg - 90;

            return Matrix4x4.TRS(
                new Vector3(newMidBaselinePos.x, newMidBaselinePos.y, 0),
                Quaternion.AngleAxis(rotationAngle, Vector3.forward),
                Vector3.one
            );
        }
    }
}
