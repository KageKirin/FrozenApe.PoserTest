using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrozenAPE;
using Unity.Serialization.Json;
using UnityEngine;

namespace Game
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        public enum State
        {
            Idle = 0,
            Pose,
            Freeze,
            Export,
            Exit,
        }

        public State state = State.Idle;
        public GameObject targetGameObject;
        public PosedBoneContainer posedBones;

        void Start()
        {
            targetGameObject = GameObject.Find("Ethan");
            Debug.AssertFormat(
                targetGameObject != null,
                "could not retrieve GameObject {0}",
                targetGameObject
            );

            var path = $"{Application.dataPath}/apose.json";
            var json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"Failed to read {path}.");
                return;
            }

            posedBones = JsonSerialization.FromJson<PosedBoneContainer>(json);
            if (posedBones == null || posedBones.bones.Count == 0)
            {
                Debug.LogError($"Could not read posed bones in {path}.");
                return;
            }

            if (targetGameObject != null)
            {
                state = State.Pose;
            }
        }

        void Update()
        {
            if (state == State.Idle)
                return;

            if (state == State.Pose)
            {
                IRigPuppeteer rigPuppeteer = new RigPuppeteer();
                var transforms = targetGameObject.GetComponentsInChildren<Transform>(
                    includeInactive: true
                );

                rigPuppeteer.Pose(transforms, posedBones.bones);

                state = State.Idle;
            }
    }
}
