/**************************************************************************************************

Environmental Acoustics version 0.2
By Syed Assad Abbas Bokhari (2023)
www.assadbokhari.com

A simple Unity C# script which calculates the dimension of a space.
Currently it alters two Wwise RTPC's which adjust the send reverb dynamically.

This can be altered to use FMOD or another way to dynamically change the reverb settings.
Adjust the framesPerUpdate to a higher number and rayCount to a lower number to improve performance.

***************************************************************************************************/

using UnityEngine;

public class AcousticAnalyser : MonoBehaviour
{
    private CharacterController _charController;
    private bool _isMoving = false;
    private Vector3[] _rayDirections;
    private int _frameCounter = 0;

    [Header("Room Data")]
    private float _averageHeight, _averageDistance;
    private float[] _distances;
    private float[] _ceilingHeights;
    
    public Vector3 spaceDimensions;
    public float roomSize;
    
    [Header("Performance Settings")]
    
    public float playerVelocityThreshold = 0.1f;
    public int rayCount = 8, rotationSteps;
    public float maxDistance = 50f, maxHeight = 100f, ceilingRayOffset = 2f;
    public int framesPerUpdate = 3;
    public bool drawDebugLines = false;
    public bool isInside = false;

    private void Start()
    {
        _charController = GetComponentInParent<CharacterController>();
        _rayDirections = CalculateRayDirections();
        _distances = new float[_rayDirections.Length];
        _ceilingHeights = new float[_rayDirections.Length];
    }

    private void Update()
    {
        _frameCounter++;

        if (_frameCounter <= framesPerUpdate) return;
        else
        {
            _isMoving = _charController.velocity.magnitude > playerVelocityThreshold;

            if (_isMoving) CastAllRays();

            CalculateSpaceDimensions();
            roomSize = _averageHeight * _averageDistance;
            if (!isInside)
            {
                AkSoundEngine.SetRTPCValue("Outside_Room_Size", roomSize);
                AkSoundEngine.SetRTPCValue("Inside_Room_Size", -1f);
            }
            else
            {
                AkSoundEngine.SetRTPCValue("Inside_Room_Size", roomSize);
                AkSoundEngine.SetRTPCValue("Outside_Room_Size", -1f);
            }
            _frameCounter = 0;
        }
    }

    private void CastAllRays()
    {
        for (int i = 0; i < _distances.Length; i++)
        {
            CastDistanceRay(i);
            CastCeilingRay(i);
        }
        _averageDistance = CalculateAverage(_distances);
        _averageHeight = CalculateAverage(_ceilingHeights);
    }

    private void CastDistanceRay(int index)
    {
        RaycastHit hit;
        Vector3 direction = _rayDirections[index];
        
        if (drawDebugLines) Debug.DrawRay(transform.position, direction * maxDistance, Color.red);
        
        Ray ray = new Ray(transform.position, direction);

        if (Physics.Raycast(ray, out hit, maxDistance)) _distances[index] = hit.distance;
        else _distances[index] = maxDistance;
    }

    private void CastCeilingRay(int index)
    {
        RaycastHit hit;
        Vector3 endPoint = transform.position + _rayDirections[index] * (_distances[index]/ceilingRayOffset);
        Vector3 ceilingDirection = Vector3.up;
        Ray ceilingRay = new Ray(endPoint, ceilingDirection);

        if (Physics.Raycast(ceilingRay, out hit, maxDistance))
        {
            if (hit.point.y - transform.position.y <= maxHeight) _ceilingHeights[index] = hit.point.y;
            else _ceilingHeights[index] = transform.position.y + maxHeight;
        }
        else _ceilingHeights[index] = transform.position.y + maxHeight;
        
        if(drawDebugLines) Debug.DrawRay(endPoint, ceilingDirection * maxDistance, Color.blue);
    }

    private Vector3[] CalculateRayDirections()
    {
        Vector3[] directions = new Vector3[rayCount * rotationSteps];
        float angleIncrement = 360f / rayCount;

        for (int step = 0; step < rotationSteps; step++)
        {
            Quaternion rotation = Quaternion.Euler(0f, step * (360f / rotationSteps), 0f);
            int offset = step * rayCount;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * angleIncrement;
                Vector3 direction = rotation * Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                directions[offset + i] = direction;
            }
        }
        return directions;
    }
    
    private void CalculateSpaceDimensions()
    {
        float minX = Mathf.Infinity;
        float maxX = -Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxY = -Mathf.Infinity;
        float minZ = Mathf.Infinity;
        float maxZ = -Mathf.Infinity;

        for (int i = 0; i < _distances.Length; i++)
        {
            Vector3 rayDirection = _rayDirections[i] * _distances[i];
            Vector3 rayEndPosition = transform.position + rayDirection;

            minX = Mathf.Min(minX, rayEndPosition.x);
            maxX = Mathf.Max(maxX, rayEndPosition.x);
            minZ = Mathf.Min(minZ, rayEndPosition.z);
            maxZ = Mathf.Max(maxZ, rayEndPosition.z);

            minY = Mathf.Min(minY, _ceilingHeights[i]);
            maxY = Mathf.Max(maxY, _ceilingHeights[i]);
        }
        
        float width = maxX - minX;
        float height = _averageHeight/2;
        float depth = maxZ - minZ;
        spaceDimensions = new Vector3(width, height, depth);
        
        if (drawDebugLines)
        {
            Color spaceColor = Color.green;
            DrawDebugSpace(transform.position, spaceDimensions, spaceColor);
        }
    }

    private void DrawDebugSpace(Vector3 center, Vector3 dimensions, Color color)
    {
        Vector3 halfDimensions = dimensions * 0.5f;

        Vector3 frontTopLeft = center + new Vector3(-halfDimensions.x, halfDimensions.y, -halfDimensions.z);
        Vector3 frontTopRight = center + new Vector3(halfDimensions.x, halfDimensions.y, -halfDimensions.z);
        Vector3 frontBottomLeft = center + new Vector3(-halfDimensions.x, -halfDimensions.y, -halfDimensions.z);
        Vector3 frontBottomRight = center + new Vector3(halfDimensions.x, -halfDimensions.y, -halfDimensions.z);

        Vector3 backTopLeft = center + new Vector3(-halfDimensions.x, halfDimensions.y, halfDimensions.z);
        Vector3 backTopRight = center + new Vector3(halfDimensions.x, halfDimensions.y, halfDimensions.z);
        Vector3 backBottomLeft = center + new Vector3(-halfDimensions.x, -halfDimensions.y, halfDimensions.z);
        Vector3 backBottomRight = center + new Vector3(halfDimensions.x, -halfDimensions.y, halfDimensions.z);

        Debug.DrawLine(frontTopLeft, frontTopRight, color);
        Debug.DrawLine(frontTopRight, frontBottomRight, color);
        Debug.DrawLine(frontBottomRight, frontBottomLeft, color);
        Debug.DrawLine(frontBottomLeft, frontTopLeft, color);

        Debug.DrawLine(backTopLeft, backTopRight, color);
        Debug.DrawLine(backTopRight, backBottomRight, color);
        Debug.DrawLine(backBottomRight, backBottomLeft, color);
        Debug.DrawLine(backBottomLeft, backTopLeft, color);

        Debug.DrawLine(frontTopLeft, backTopLeft, color);
        Debug.DrawLine(frontTopRight, backTopRight, color);
        Debug.DrawLine(frontBottomRight, backBottomRight, color);
        Debug.DrawLine(frontBottomLeft, backBottomLeft, color);
    }

    private float CalculateAverage(float[] array)
    {
        if (array.Length == 0) return 0;
        
        float sum = 0f;
        for (int i = 0; i < array.Length; i++) sum += array[i];

        float average = sum / array.Length;
        return average;
    }
}
