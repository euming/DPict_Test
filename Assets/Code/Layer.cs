using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable] // Required so it shows up in the inspector 
[AddComponentMenu ("DPict/Layer")]
public class Layer : MonoBehaviour 
{
	Texture2D			m_myTexture;
	Camera				m_myCamera = null;
	Color[]				m_pixelLayer;
	public int			m_textureWidth;
	public int			m_textureHeight;
	int					m_maxPoints=6;
	Vector3[]			m_prevPoints = null;
	int					m_frameCounter;
	bool				m_bIsDrawing;
	float				m_BrushLineDensity = 3.5f;

	/*
	//	brush stuff
	float				m_blendValue = 0.333f;
	int					m_brushWidth = 8;
	Color 				m_brushColor = Color.blue;
	Color[] 			color_palette;
	int					m_curColorIndex = 0;
	int 				m_maxColors = 7;
	*/
	
	public Brush		m_myBrush;
	
	static Layer		m_currentLayer;
	static List<Layer>	m_layerList = new List<Layer>();
	
	void Awake()
	{
		GameObject camGO = GameObject.FindGameObjectWithTag("OrthoCamera");
		if (camGO != null) {
			m_myCamera = camGO.camera;
		}
		/*
		color_palette = new Color[m_maxColors];
		color_palette[0] = Color.blue;
		color_palette[1] = Color.red;
		color_palette[2] = Color.green;
		color_palette[3] = Color.yellow;
		color_palette[4] = Color.white;
		color_palette[5] = Color.black;
		color_palette[6] = Color.gray;
		*/
		
		if (this.tag == "DrawingLayer") {
			m_currentLayer = this;
		}
		m_layerList.Add(this);
		m_bIsDrawing = false;
	
		m_prevPoints = new Vector3[m_maxPoints];
	    renderer.material.mainTexture = InstantiateTexture();
	}
	
	Texture2D InstantiateTexture()
	{
		Texture2D texture = Instantiate(renderer.material.mainTexture) as Texture2D;
	    
		m_myTexture = texture;
		m_textureWidth = texture.width;
		m_textureHeight = texture.height;
		
		Clear(texture, 0, Color.white);
		
		m_pixelLayer = texture.GetPixels(0);
		
		return texture;
	}
	
	// Use this for initialization
	void Start () 
	{
	
		//m_prevPoints = new Vector3[m_maxPoints];
		m_frameCounter = 0;
	}
	
	static public Brush GetBrush()
	{
		return m_currentLayer.m_myBrush;
	}
	
	static public void SetBrush(Brush brush)
	{
		m_currentLayer.m_myBrush = brush;
	}
	
	static public void SetBrushColor(Color brushColor)
	{
		//m_currentLayer.m_brushColor = brushColor;
		m_currentLayer.m_myBrush.SetBrushColor(brushColor);
	}
	
	static public void SetBrushSize(int sz)
	{
		//m_currentLayer.m_brushWidth = sz;
		m_currentLayer.m_myBrush.SetBrushSize(sz);
	}
	
	public void OnMouseDown()
	{
		m_bIsDrawing = true;
		StartPoint(GetPoint());
	}
	
	public void OnMouseUp()
	{
		DrawSegments();
		m_bIsDrawing = false;
	}
	
	public void PaintBrush(Vector3 pos, Brush brush)
	{
		Color color = brush.GetBrushColor();
		int brushWidth = brush.GetBrushSize();
		float blendValue = brush.GetBlendValue();
		bool bSoftEdges = true;
		if (brush.m_BrushStyle == Brush.BrushStyle.BrushStyle_HardEdges) {
			bSoftEdges = false;
		}
		PaintBrush(pos, color, brushWidth, blendValue, bSoftEdges);
	}
	
	void PaintBrush(Vector3 pos, Color color, int brushWidth, float brushBlendValue, bool bSoftEdges)
	{
		int xStart = (int)pos.x;
		int yStart = (int)pos.y;
		float brushWidthRadiusSquared = (float)(brushWidth*brushWidth);
		float ratio;
		float fRadiusSq;
		float blendValue;
		
		for(int yy=-brushWidth; yy<=+brushWidth; yy++) {
			for(int xx=-brushWidth; xx<=+brushWidth; xx++) {
				if (xx*xx+yy*yy<=brushWidth*brushWidth) {
					if ((xx+xStart>=0) && (yy+yStart>=0) && (xx+xStart<m_textureWidth) && (yy+yStart<m_textureHeight)) {
						if (bSoftEdges == true) {
							fRadiusSq = (float)(xx*xx+yy*yy);
							ratio = fRadiusSq/brushWidthRadiusSquared;
							blendValue = (1.0f-ratio) * brushBlendValue;
						}
						else {
							blendValue = 1.0f * brushBlendValue;
						}
						m_pixelLayer[xx+xStart + (yy+yStart)*m_textureWidth] = Color.Lerp(m_pixelLayer[xx+xStart + (yy+yStart)*m_textureWidth], color, blendValue);
					}
				}
			}
		}
	}
	
	public void StartPoint(Vector3 pt)
	{
		for(int ii=0; ii<m_prevPoints.Length; ii++) {
			m_prevPoints[ii] = pt;
		}
	}
	
	void AddPreviousPoint(Vector3 pt)
	{
		for(int ii=0; ii<m_prevPoints.Length-1; ii++) {
			m_prevPoints[ii] = m_prevPoints[ii+1];
		}
		m_prevPoints[m_prevPoints.Length-1] = pt;
	}
	
	void InterpolatePreviousPoints(int idx0, int idx1)
	{
		Vector3 newPt;
		float fBrushWidth = (float)m_myBrush.GetBrushSize();
		
		float	interpolationAmount = 0.1f;
		float 	distBetweenPts;
		float	nInterpolations;
		
		for(int ii=idx0; ii<idx1; ii++) {
			for(float p=0.0f; p<1.0f; p+=interpolationAmount) {
				distBetweenPts = Vector3.Distance(m_prevPoints[ii], m_prevPoints[ii+1]);
				nInterpolations = m_BrushLineDensity * distBetweenPts / fBrushWidth;
				interpolationAmount = 1.0f / nInterpolations;
				newPt = Vector3.Lerp(m_prevPoints[ii], m_prevPoints[ii+1], p);
				PaintBrush(newPt, m_myBrush);
			}
		}
	}
	
	Vector3 GetPoint()
	{
		Vector3 hitPoint = Vector3.zero;
		Ray ray = m_myCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		
		if (Physics.Raycast(ray, out hit, m_myCamera.farClipPlane, m_myCamera.cullingMask)) {
			hitPoint = hit.point;
			hitPoint.x = 1024 - (hit.point.x + 512);
			hitPoint.y = 1024 - (hit.point.y + 512);
		}
		else {
			hitPoint = m_prevPoints[m_prevPoints.Length-1];		//	use previous point if we're off screen
		}
		return hitPoint;
	}
	
	public void DrawSegments()
	{
		int		buffer = 2;
		int		nSegments = 3;
		
		InterpolatePreviousPoints(m_maxPoints-buffer-nSegments, m_maxPoints-buffer);
		m_myTexture.SetPixels(m_pixelLayer);
		m_myTexture.Apply();
	}
	
	public void Clear()
	{
		Clear(this.m_myTexture, 0, Color.white);
	}
	
	public void Clear(Texture2D texture, int mip, Color color)
	{
	    // tint each mip level
		Color[] pixelLayer = m_pixelLayer;
		if (m_pixelLayer==null)
			pixelLayer = texture.GetPixels(mip);
        var cols = pixelLayer;
        for( var i = 0; i < cols.Length; ++i ) {
            //	cols[i] = Color.Lerp( cols[i], colors[mip], 0.33f );
			cols[i] = color;
        }
        texture.SetPixels( cols, mip );
	
	    // actually apply all SetPixels, don't recalculate mip levels
	    texture.Apply( false );
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (m_bIsDrawing && (Input.GetMouseButton(0) == true)) {

			AddPreviousPoint(GetPoint());

			if (m_frameCounter==3) {
				DrawSegments();
				m_frameCounter = 0;
			}
			m_frameCounter++;
		}
		
		/*
		//	right clicked
		if (Input.GetMouseButtonDown(1) == true) {
			m_curColorIndex++;
			if (m_curColorIndex>=m_maxColors) {
				m_curColorIndex = 0;
			}
			m_brushColor = color_palette[m_curColorIndex];
		}
		*/
	}
}
