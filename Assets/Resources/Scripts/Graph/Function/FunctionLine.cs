﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

public class FunctionLine {
    public static Canvas functionCanvas;
    public static Texture lineTexture;
    public static Material lineMaterial;
    protected float lineWidth = 10f * (Screen.width / 1280f);
    protected Color lineColor;
    protected float currDrawStart;  //0 to 1
    protected float currDrawEnd;  //0 to 1
    public FunctionLine(Color c) {
        lineColor = c;
        initLine();
    }
    public virtual void initLine() { }
    public virtual void cachePoints(Equation eq, float startInput, float endInput, int numPoints) { }
    public virtual List<Vector3> getAllPoints() {
        return new List<Vector3>();
    }
    public void drawLines() {
        drawLines(currDrawStart, currDrawEnd);
    }
    public virtual void drawLines(float start, float end) { } // 0 <= start <= end <= 1
    public void setColor(Color c) {
        lineColor = c;
    }
    public virtual void destroyLines() { }
}

public class DiscontinuousLine : FunctionLine {
    private List<List<Vector3>> originalPoints;
    private List<bool> isHole;
    private List<bool> isFilled;
    private List<VectorLine> visualLines;
    private List<VectorLine> colliderLines;

    public DiscontinuousLine(Color c) : base(c) { }
    public override void initLine() {
        originalPoints = new List<List<Vector3>>();
        visualLines = new List<VectorLine>();
        colliderLines = new List<VectorLine>();
        isHole = new List<bool>();
        isFilled = new List<bool>();
    }
    public override void cachePoints(Equation eq, float startInput, float endInput, int numPoints) {
        float interval = (endInput - startInput) / numPoints;
        createLine();
        for (float prevInput = startInput; prevInput <= endInput; prevInput += interval) {
            float currInput = prevInput + interval;
            Discontinuity d = eq.checkDiscontinuity(prevInput, currInput);
            if (d != null && d.param != currInput) {
                if (d.param != prevInput) originalPoints.Last().Add(eq.getPoint(prevInput));
                for (int i = 0; i < d.holes.Count; i++) createHole(d.holes[i], d.isFilled[i]);
                createLine();
            }
            else originalPoints.Last().Add(eq.getPoint(prevInput));
        }
    }
    public override List<Vector3> getAllPoints() {
        List<Vector3> allPoints = new List<Vector3>();
        for (int i = 0; i < originalPoints.Count; i++) {
            for (int j = 0; j < originalPoints[i].Count; j++) allPoints.Add(originalPoints[i][j]);
        }
        return allPoints;
    }
    private void createLine() {
        originalPoints.Add(new List<Vector3>());
        isHole.Add(false);
        isFilled.Add(false);

        VectorLine visualLine = new VectorLine("VISUAL " + originalPoints.Count, new List<Vector3>(), lineTexture, lineWidth, LineType.Continuous, Joins.Fill);
        visualLine.color = lineColor;
        visualLine.material = lineMaterial;
        //line.smoothColor = true;
        visualLine.rectTransform.gameObject.tag = "Function";
        visualLine.layer = LayerMask.NameToLayer("PlayerEncounterable");
        visualLine.SetCanvas(functionCanvas, false);

        VectorLine colliderLine = new VectorLine("COLLIDER " + originalPoints.Count, new List<Vector3>(), lineWidth, LineType.Continuous, Joins.Fill);
        colliderLine.color = Color.clear;
        colliderLine.trigger = true;
        colliderLine.collider = false;
        colliderLine.rectTransform.gameObject.tag = "Function";
        colliderLine.layer = LayerMask.NameToLayer("PlayerEncounterable");
        colliderLine.SetCanvas(functionCanvas, true);

        visualLines.Add(visualLine);
        colliderLines.Add(colliderLine);
    }
    private void createHole(Vector3 hole, bool filled) {
        createLine();
        int index = originalPoints.Count - 1;
        originalPoints[index].Add(hole);
        isHole[index] = true;
        isFilled[index] = filled;
        visualLines[index].SetWidth(filled ? lineWidth * 2 : lineWidth);
        colliderLines[index].SetWidth(filled ? lineWidth * 2 : lineWidth);
    }
    public override void drawLines(float start, float end) {
        int count = getAllPoints().Count;
        int startInput = (int)(count * start);
        int endInput = (int)(count * end);
        int currInput = 0;
        //putting in the points to be drawn
        for (int i = 0; i < originalPoints.Count; i++) {
            List<Vector3> temp = new List<Vector3>();
            visualLines[i].points3 = temp;
            colliderLines[i].points3 = temp;
            if (isHole[i]) {
                if (currInput >= startInput && currInput <= endInput) {
                    Vector3 center = CartesianPlane.SharedPlane.pointRelativeToOrigin(originalPoints[i].First());
                    float radius = isFilled[i] ? 0.05f : 0.1f;
                    for (int j = 0; j < 30; j++) temp.Add(originalPoints[i].First());
                    visualLines[i].MakeCircle(center, radius);
                    colliderLines[i].MakeCircle(center, radius);
                }
                currInput++;
            }
            else {
                foreach (Vector3 point in originalPoints[i]) {
                    if (currInput >= startInput && currInput <= endInput) temp.Add(CartesianPlane.SharedPlane.pointRelativeToOrigin(point));
                    currInput++;
                }
            }
            colliderLines[i].collider = colliderLines[i].points3.Count > 1;
        }
        //cancer hole stuff (this removes the collider too somehow)
        for (int i = 0; i < isHole.Count; i++) {
            if (isHole[i] && !isFilled[i]) {
                for (int k = 0; k < visualLines.Count; k++) {
                    if (!isHole[k]) {
                        for (int j = 0; j < visualLines[k].points3.Count; j++) {
                            if (Vector3.Distance(visualLines[k].points3[j], CartesianPlane.SharedPlane.pointRelativeToOrigin(originalPoints[i].First())) <= 0.1f) {
                                visualLines[k].points3.RemoveAt(j--);
                            }
                        }
                    }
                }
            }
        }
        //actually drawing the lines
        foreach (VectorLine line in visualLines) line.Draw();
        foreach (VectorLine line in colliderLines) line.Draw();
        currDrawStart = start;
        currDrawEnd = end;
    }
    public override void destroyLines() {
        VectorLine.Destroy(visualLines);
        VectorLine.Destroy(colliderLines);
        initLine();
    }
    public bool hasHoles() {
        foreach (bool hole in isHole) if (hole) return true;
        return false;
    }
}
public class ContinuousLine : FunctionLine {
    private List<Vector3> originalPoints;
    private VectorLine visualLine;
    private VectorLine colliderLine;

    public ContinuousLine(Color c) : base(c) { }
    public override void initLine() {
        originalPoints = new List<Vector3>();

        visualLine = new VectorLine("Continuous Line Visual", new List<Vector3>(), lineTexture, lineWidth, LineType.Continuous, Joins.Fill);
        visualLine.material = lineMaterial;
        visualLine.color = lineColor;
        visualLine.rectTransform.gameObject.tag = "Function";
        visualLine.layer = LayerMask.NameToLayer("PlayerEncounterable");
        visualLine.SetCanvas(functionCanvas, false);

        colliderLine = new VectorLine("Continuous Line Collider", new List<Vector3>(), lineWidth, LineType.Continuous, Joins.Fill);
        colliderLine.color = Color.clear;
        colliderLine.trigger = true;
        colliderLine.collider = false;
        colliderLine.rectTransform.gameObject.tag = "Function";
        colliderLine.layer = LayerMask.NameToLayer("PlayerEncounterable");
        colliderLine.SetCanvas(functionCanvas, true);
    }
    public override void cachePoints(Equation eq, float startInput, float endInput, int numPoints) {
        float interval = (endInput - startInput) / numPoints;
        for (float currInput = startInput; currInput <= endInput; currInput += interval) originalPoints.Add(eq.getPoint(currInput));
    }
    public override List<Vector3> getAllPoints() {
        return originalPoints;
    }
    public override void drawLines(float start, float end) {
        int startIndex = (int)(start * originalPoints.Count);
        int endIndex = (int)(end * originalPoints.Count);

        List<Vector3> temp = new List<Vector3>();
        visualLine.points3 = temp;
        colliderLine.points3 = temp;
        for (int i = startIndex; i < endIndex; i++) temp.Add(CartesianPlane.SharedPlane.pointRelativeToOrigin(originalPoints[i]));
        colliderLine.collider = colliderLine.points3.Count > 1;
        visualLine.Draw();
        colliderLine.Draw();

        currDrawStart = startIndex;
        currDrawEnd = endIndex;
    }
    public override void destroyLines() {
        VectorLine.Destroy(ref visualLine);
        VectorLine.Destroy(ref colliderLine);
        initLine();
    }
    public IEnumerator pulseColor(Color newColor, float pulseTime, float thickness, bool right) {  //sus
        for (float time = 0f; time <= pulseTime; time += Time.deltaTime) {
            float ratio = right ? time / pulseTime : (pulseTime - time) / pulseTime;
            int segments = (originalPoints.Count - 1);
            int center = (int)(ratio * segments);
            int interval = (int)(thickness * segments);

            visualLine.SetColor(lineColor);
            for (int i = center - interval / 2; i <= center + interval / 2; i++) {
                if (i >= 0 && i < segments) {
                    float colorRatio = (float)Mathf.Abs(i - center) / (interval / 2);
                    visualLine.SetColor(newColor * (1 - colorRatio) + lineColor * (colorRatio), i);
                }
            }
            yield return null;
        }
        visualLine.SetColor(lineColor);
    }
}