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
        public GameObject frozenGameObject;
        public TextAsset poseJson;
        public PosedBoneContainer posedBones;

        void Start()
        {
            targetGameObject = GameObject.Find("Ethan");
            Debug.AssertFormat(
                targetGameObject != null,
                "could not retrieve GameObject {0}",
                targetGameObject
            );

            Debug.AssertFormat(poseJson != null, "pose.json is not set {0}", poseJson);
            Debug.AssertFormat(poseJson.text != string.Empty, "pose.json is empty {0}", poseJson);

            posedBones = JsonSerialization.FromJson<PosedBoneContainer>(poseJson.text);
            if (posedBones == null || posedBones.bones.Count == 0)
            {
                Debug.LogError($"Could not read posed bones in {poseJson.name}.");
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
                Debug.Log($"posing {targetGameObject.name}");

                IRigPuppeteer rigPuppeteer = new RigPuppeteer();
                var transforms = targetGameObject.GetComponentsInChildren<Transform>(
                    includeInactive: true
                );

                rigPuppeteer.Pose(transforms, posedBones.bones);

                state = State.Freeze;
            }

            if (state == State.Freeze)
            {
                Debug.Log($"freezing {targetGameObject.name}");

                IPoseFreezer poseFreezer = new PoseFreezer();
                var frozenMaterials = poseFreezer.Freeze(targetGameObject);
                frozenGameObject = new() { name = $"frozen_{targetGameObject.name}" };
                frozenGameObject.transform.SetPositionAndRotation(
                    targetGameObject.transform.position,
                    targetGameObject.transform.rotation
                );
                frozenGameObject.transform.localScale = targetGameObject.transform.localScale;

                foreach (var meshMaterials in frozenMaterials)
                {
                    var (mesh, materials) = meshMaterials;
                    GameObject meshObject = new($"frozen_{mesh.name}");
                    meshObject.transform.parent = frozenGameObject.transform;

                    var meshFilter = meshObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;

                    var meshRenderer = meshObject.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterials = materials;
                }

                if (frozenGameObject == null)
                {
                    state = State.Idle;
                }
                else
                {
                    state = State.Export;
                }
            }

            if (state == State.Export)
            {
                Debug.Log($"exporting {frozenGameObject.name}");

                ITextureWriter texWriter = new TextureTGAWriter();
                IWavefrontOBJWriter objWriter = new WavefrontOBJWriter();
                IWavefrontMTLWriter mtlWriter = new WavefrontMTLWriter(texWriter);

                foreach (
                    var meshFilter in frozenGameObject.GetComponentsInChildren<MeshFilter>(true)
                )
                {
                    if (meshFilter.sharedMesh == null)
                        continue;

                    var meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                    var targetPathObj =
                        $"{Directory.GetCurrentDirectory()}/EthanPose_{meshFilter.gameObject.name}.obj";
                    var targetPathMtl =
                        $"{Directory.GetCurrentDirectory()}/EthanPose_{meshFilter.gameObject.name}.mtl";
                    var materials =
                        meshRenderer != null
                            ? meshRenderer.sharedMaterials
                            : Array.Empty<Material>();

                    Debug.Log($"writing {targetPathObj}");
                    var obj = objWriter.WriteOBJ(
                        Path.GetFileNameWithoutExtension(targetPathObj),
                        meshFilter.sharedMesh,
                        materials
                    );
                    File.WriteAllText(targetPathObj, obj);

                    Debug.Log($"writing {targetPathMtl}");
                    var mtl = mtlWriter.WriteMTL(
                        Path.GetFileNameWithoutExtension(targetPathMtl),
                        materials
                    );
                    File.WriteAllText(targetPathMtl, mtl);

                    var textures = materials.Select(x => x.mainTexture).Where(x => x != null);
                    foreach (var tex in textures)
                    {
                        var targetPathTex = $"{Directory.GetCurrentDirectory()}/{texWriter.NameTexture(tex)}";
                        Debug.Log($"writing {targetPathTex}");
                        try
                        {
                            var buf = texWriter.WriteTexture(tex);
                            File.WriteAllBytes(targetPathTex, buf);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"failed to export {targetPathTex}: {ex}");
                        }
                    }
                }

                foreach (
                    var skinnedMeshRenderer in frozenGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(
                        true
                    )
                )
                {
                    if (skinnedMeshRenderer.sharedMesh == null)
                        continue;

                    var targetPathObj =
                        $"{Directory.GetCurrentDirectory()}/EthanPose_{skinnedMeshRenderer.gameObject.name}.obj";
                    var targetPathMtl =
                        $"{Directory.GetCurrentDirectory()}/EthanPose_{skinnedMeshRenderer.gameObject.name}.mtl";

                    Debug.Log($"writing {targetPathObj}");
                    var obj = objWriter.WriteOBJ(
                        Path.GetFileNameWithoutExtension(targetPathObj),
                        skinnedMeshRenderer.sharedMesh,
                        skinnedMeshRenderer.sharedMaterials
                    );
                    File.WriteAllText(targetPathObj, obj);

                    Debug.Log($"writing {targetPathMtl}");
                    var mtl = mtlWriter.WriteMTL(
                        Path.GetFileNameWithoutExtension(targetPathMtl),
                        skinnedMeshRenderer.sharedMaterials
                    );
                    File.WriteAllText(targetPathMtl, mtl);

                    var textures = skinnedMeshRenderer.sharedMaterials.Select(x => x.mainTexture).Where(x => x != null);
                    foreach (var tex in textures)
                    {
                        var targetPathTex = $"{Directory.GetCurrentDirectory()}/{texWriter.NameTexture(tex)}";
                        Debug.Log($"writing {targetPathTex}");
                        try
                        {
                            var buf = texWriter.WriteTexture(tex);
                            File.WriteAllBytes(targetPathTex, buf);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"failed to export {targetPathTex}: {ex}");
                        }
                    }
                }

                state = State.Exit;
            }

            if (state == State.Exit)
            {
                if (Application.isBatchMode)
                {
                    Application.Quit();
                    state = State.Idle; //until exit
                }
            }
        }
    }
}
