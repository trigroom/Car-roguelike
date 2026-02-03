using System.Collections.Generic;
using UnityEngine;

struct TrainControllerComponent {
    public List<Transform> TrainRails; //check position and local rotate
    public Transform TrainTransform;
    public float TimeToNextTrain;
}
