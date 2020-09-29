using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class PushupController : CharaCustomFunctionController
        {
            public BodyData BaseData;
            public BodyData LoadedBaseData;
            public BodyData CurrentPushupData;

            private Dictionary<int, ClothData> BraDataDictionary = new Dictionary<int, ClothData>();
            private Dictionary<int, ClothData> TopDataDictionary = new Dictionary<int, ClothData>();

#if KK
            //EC only has one outfit slot, no need to watch changes
            protected override void Start()
            {
                CurrentCoordinate.Subscribe(value => { OnCoordinateChanged(); });
                base.Start();
            }
#endif
            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                MapBodyInfoToChaFile(BaseData);

                var data = new PluginData();
                data.data.Add(PushupConstants.Pushup_BraData, MessagePackSerializer.Serialize(BraDataDictionary));
                data.data.Add(PushupConstants.Pushup_TopData, MessagePackSerializer.Serialize(TopDataDictionary));
                data.data.Add(PushupConstants.Pushup_BodyData, MessagePackSerializer.Serialize(BaseData));
                SetExtendedData(data);

                RecalculateBody(true, true);
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                bool hasSavedBaseData = false;
                var flags = MakerAPI.GetCharacterLoadFlags();
                bool clothesFlag = flags == null || flags.Clothes;
                bool bodyFlag = flags == null || flags.Body;

                if (bodyFlag)
                {
                    BaseData = new BodyData(ChaControl.fileBody);
                    LoadedBaseData = new BodyData(ChaControl.fileBody);
                    BaseData.CopyTo(LoadedBaseData);
                    CurrentPushupData = new BodyData(ChaControl.fileBody);
                }

                if (clothesFlag)
                {
                    //Load the data only if clothes is checked to be loaded
                    BraDataDictionary = new Dictionary<int, ClothData>();
                    TopDataDictionary = new Dictionary<int, ClothData>();

                    var data = GetExtendedData();
                    if (data != null && data.data.TryGetValue(PushupConstants.Pushup_BraData, out var loadedBraData) && loadedBraData != null)
                        BraDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);

                    if (data != null && data.data.TryGetValue(PushupConstants.Pushup_TopData, out var loadedTopData) && loadedTopData != null)
                        TopDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);

                    if (data != null && data.data.TryGetValue(PushupConstants.Pushup_BodyData, out var loadedBodyData) && loadedBodyData != null)
                    {
                        hasSavedBaseData = true;
                        LoadedBaseData = MessagePackSerializer.Deserialize<BodyData>((byte[])loadedBodyData);
                    }

                    //Reset advanced mode stuff and disable it when not loading the body in character maker
                    if (!bodyFlag)
                    {
                        foreach (var clothData in BraDataDictionary.Values)
                        {
                            BaseData.CopyTo(clothData);
                            clothData.UseAdvanced = false;
                        }
                        foreach (var clothData in TopDataDictionary.Values)
                        {
                            BaseData.CopyTo(clothData);
                            clothData.UseAdvanced = false;
                        }
                    }
                    if (!hasSavedBaseData)
                    {
                        if (data?.data != null)
                        {
                            data.data.Add(PushupConstants.Pushup_BodyData, MessagePackSerializer.Serialize(BaseData));
                            SetExtendedData(data);
                        }
                    }
                }

                //Apply the saved data to the base body data since it sometimes gets overwritten in the main game
                if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame)
                    LoadedBaseData.CopyTo(BaseData);

                RecalculateBody();
                base.OnReload(currentGameMode);
            }

            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {
                var data = new PluginData();
                data.data.Add(PushupConstants.PushupCoordinate_BraData, MessagePackSerializer.Serialize(CurrentBraData));
                data.data.Add(PushupConstants.PushupCoordinate_TopData, MessagePackSerializer.Serialize(CurrentTopData));
                SetCoordinateExtendedData(coordinate, data);
            }

            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
            {
                if (MakerAPI.GetCoordinateLoadFlags()?.Clothes == false) return;

                ClothData newBraData = new ClothData(CurrentBraData);
                ClothData newTopData = new ClothData(CurrentTopData);

                var data = GetCoordinateExtendedData(coordinate);
                if (data != null && data.data.TryGetValue(PushupConstants.PushupCoordinate_BraData, out var loadedBraData) && loadedBraData != null)
                {
                    try
                    {
                        newBraData = MessagePackSerializer.Deserialize<ClothData>((byte[])loadedBraData);
                    }
                    catch
                    {
                        Logger.LogError("Error loading coordinate");
                    }
                }

                if (data != null && data.data.TryGetValue(PushupConstants.PushupCoordinate_TopData, out var loadedTopData) && loadedTopData != null)
                {
                    try
                    {
                        newTopData = MessagePackSerializer.Deserialize<ClothData>((byte[])loadedTopData);
                    }
                    catch
                    {
                        Logger.LogError("Error loading coordinate");
                    }
                }
                else
                    CurrentTopData = null;

                //Copy the cloth data but not body data
                newBraData.CopyTo(CurrentBraData);
                newTopData.CopyTo(CurrentTopData);

                //Advanced mode data is too character specific and is not loaded from coordinates
                CurrentBraData.UseAdvanced = false;
                if (CurrentTopData != null)
                    CurrentTopData.UseAdvanced = false;

                RecalculateBody();
            }

#if KK
            private static void OnCoordinateChanged()
            {
                if (MakerAPI.InsideAndLoaded)
                    ReloadPushup();
            }
#endif
            /// <summary>
            /// Recalculate the body based on the clothing state and Pushup settings.
            /// </summary>
            /// <param name="recalculateIfCharacterLoading">Whether to perform recalculation when the character is in the process of loading. Set this to false if it would cause problems.</param>
            /// <param name="coroutine">If true, wait one frame before recalculating.</param>
            public void RecalculateBody(bool recalculateIfCharacterLoading = true, bool coroutine = false)
            {
                if (CharacterLoading && !recalculateIfCharacterLoading) return;

                //Body will sometimes be null in main game, wait for it to not be null
                if (ChaControl.objBody == null)
                    coroutine = true;

                if (coroutine)
                {
                    StartCoroutine(RecalculateBodyCoroutine());
                    return;
                }

                if (MakerAPI.InsideMaker && !MakerAPI.InsideAndLoaded) return;

                //Set all the sliders to the base body values
                SliderManager.SlidersActive = true;
                MapBodyInfoToChaFile(BaseData);

                Wearing nowWearing = CurrentlyWearing;
                if (nowWearing != Wearing.None)
                {
                    SliderManager.SlidersActive = false;
                    CalculatePush(nowWearing);
                    MapBodyInfoToChaFile(CurrentPushupData);
                }
                SliderManager.SlidersActive = true;
            }

            private IEnumerator RecalculateBodyCoroutine()
            {
                yield return null;
                RecalculateBody(false);
            }

            /// <summary>
            /// Triggered when clothing state is changed, i.e. pulled aside or taken off.
            /// </summary>
            internal void ClothesStateChangeEvent()
            {
                if (!Started) return;
                RecalculateBody(false);
                UpdateABMX();
            }

            /// <summary>
            /// Triggered when clothing is changed in the character maker
            /// </summary>
            internal void ClothesChangeEvent() => RecalculateBody();

            /// <summary>
            /// Refreshes ABMX modifications
            /// </summary>
            private void UpdateABMX()
            {
                var abmxType = Type.GetType("KKABMX.Core.BoneController, KKABMX");
                if (abmxType == null) return;
                var abmxComponent = ChaControl.gameObject.GetComponent(abmxType);
                if (abmxComponent == null) return;
                Traverse.Create(abmxComponent).Property("NeedsBaselineUpdate")?.SetValue(true);
            }

            /// <summary>
            /// Sets the body values to the values stored in the BodyData.
            /// </summary>
            internal void MapBodyInfoToChaFile(BodyData bodyData)
            {
                void setShapeValue(int idx, float val)
                {
                    ChaControl.fileBody.shapeValueBody[idx] = val;
                    ChaControl.SetShapeBodyValue(idx, val);
                }

                ChaControl.ChangeBustSoftness(bodyData.Softness);
                ChaControl.ChangeBustGravity(bodyData.Weight);

                setShapeValue(PushupConstants.IndexSize, bodyData.Size);
                setShapeValue(PushupConstants.IndexVerticalPosition, bodyData.VerticalPosition);
                setShapeValue(PushupConstants.IndexHorizontalAngle, bodyData.HorizontalAngle);
                setShapeValue(PushupConstants.IndexHorizontalPosition, bodyData.HorizontalPosition);
                setShapeValue(PushupConstants.IndexVerticalAngle, bodyData.VerticalAngle);
                setShapeValue(PushupConstants.IndexDepth, bodyData.Depth);
                setShapeValue(PushupConstants.IndexRoundness, bodyData.Roundness);
                setShapeValue(PushupConstants.IndexAreolaDepth, bodyData.AreolaDepth);
                setShapeValue(PushupConstants.IndexNippleWidth, bodyData.NippleWidth);
                setShapeValue(PushupConstants.IndexNippleDepth, bodyData.NippleDepth);
            }

            internal void CalculatePush(Wearing wearing)
            {
                if (wearing == Wearing.Bra)
                {
                    CalculatePushFromClothes(CurrentBraData, CurrentBraData.UseAdvanced);
                    return;
                }
                if (wearing == Wearing.Top)
                {
                    CalculatePushFromClothes(CurrentTopData, CurrentTopData.UseAdvanced);
                    return;
                }
                if (CurrentTopData.UseAdvanced)
                {
                    CalculatePushFromClothes(CurrentTopData, true);
                    return;
                }
                if (CurrentBraData.UseAdvanced)
                {
                    CalculatePushFromClothes(CurrentBraData, true);
                    return;
                }

                var combo = new ClothData();
                combo.Firmness = Math.Max(CurrentBraData.Firmness, CurrentTopData.Firmness);
                combo.Lift = Math.Max(CurrentBraData.Lift, CurrentTopData.Lift);
                combo.Squeeze = Math.Max(CurrentBraData.Squeeze, CurrentTopData.Squeeze);
                combo.PushTogether = Math.Max(CurrentBraData.PushTogether, CurrentTopData.PushTogether);
                combo.CenterNipples = Math.Max(CurrentBraData.CenterNipples, CurrentTopData.CenterNipples);
                combo.FlattenNipples = CurrentBraData.FlattenNipples || CurrentTopData.FlattenNipples;
                combo.EnablePushup = true;

                CalculatePushFromClothes(combo, false);
            }

            internal void CalculatePushFromClothes(ClothData cData, bool useAdvanced)
            {
                if (useAdvanced)
                {
                    cData.CopyTo(CurrentPushupData);
                    return;
                }

                if (1f - cData.Firmness < BaseData.Softness)
                {
                    CurrentPushupData.Softness = 1 - cData.Firmness;
                }
                else
                {
                    CurrentPushupData.Softness = BaseData.Softness;
                }

                if (cData.Lift > BaseData.VerticalPosition)
                {
                    CurrentPushupData.VerticalPosition = cData.Lift;
                }
                else
                {
                    CurrentPushupData.VerticalPosition = BaseData.VerticalPosition;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalAngle)
                {
                    CurrentPushupData.HorizontalAngle = 1 - cData.PushTogether;
                }
                else
                {
                    CurrentPushupData.HorizontalAngle = BaseData.HorizontalAngle;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalPosition)
                {
                    CurrentPushupData.HorizontalPosition = 1 - cData.PushTogether;
                }
                else
                {
                    CurrentPushupData.HorizontalPosition = BaseData.HorizontalPosition;
                }

                if (1f - cData.Squeeze < BaseData.Depth)
                {
                    CurrentPushupData.Depth = 1 - cData.Squeeze;
                    CurrentPushupData.Size = BaseData.Size + (BaseData.Depth - (1 - cData.Squeeze)) / 10;
                }
                else
                {
                    CurrentPushupData.Depth = BaseData.Depth;
                    CurrentPushupData.Size = BaseData.Size;
                }

                if (cData.FlattenNipples)
                {
                    CurrentPushupData.NippleDepth = 0f;
                    CurrentPushupData.AreolaDepth = 0f;
                }
                else
                {
                    CurrentPushupData.NippleDepth = BaseData.NippleDepth;
                    CurrentPushupData.AreolaDepth = BaseData.AreolaDepth;
                }

                CurrentPushupData.NippleWidth = BaseData.NippleWidth;

                var nipDeviation = 0.5f - BaseData.VerticalAngle;
                CurrentPushupData.VerticalAngle = 0.5f - (nipDeviation - (nipDeviation * cData.CenterNipples));

                CurrentPushupData.Weight = BaseData.Weight;
                CurrentPushupData.Roundness = BaseData.Roundness;
            }

            public ClothData GetBraData(int coordinateIndex)
            {
                BraDataDictionary.TryGetValue(coordinateIndex, out var clothData);
                if (clothData == null)
                {
                    clothData = new ClothData(BaseData);
                    BraDataDictionary[coordinateIndex] = clothData;
                }
                return clothData;
            }
            public void SetBraData(int coordinateIndex, ClothData clothData)
            {
                if (clothData == null)
                    BraDataDictionary[coordinateIndex] = new ClothData(BaseData);
                else
                    BraDataDictionary[coordinateIndex] = clothData;
            }
            public ClothData CurrentBraData
            {
                get => GetBraData(CurrentCoordinateIndex);
                set => SetBraData(CurrentCoordinateIndex, value);
            }

            public ClothData GetTopData(int coordinateIndex)
            {
                TopDataDictionary.TryGetValue(coordinateIndex, out var clothData);
                if (clothData == null)
                {
                    clothData = new ClothData(BaseData);
                    TopDataDictionary[coordinateIndex] = clothData;
                }
                return clothData;
            }
            public void SetTopData(int coordinateIndex, ClothData clothData)
            {
                if (clothData == null)
                    TopDataDictionary[coordinateIndex] = new ClothData(BaseData);
                else
                    TopDataDictionary[coordinateIndex] = clothData;
            }
            public ClothData CurrentTopData
            {
                get => GetTopData(CurrentCoordinateIndex);
                set => SetTopData(CurrentCoordinateIndex, value);
            }

            public void CopyBraData(int coordinateSource, int coordinateDestination, bool copyClothData, bool copyBodyData)
            {
                if (coordinateSource == coordinateDestination) return;

                var src = GetBraData(coordinateSource);
                var dst = GetBraData(coordinateDestination);
                if (copyClothData)
                    src.CopyTo(dst);
                if (copyBodyData)
                    src.CopyTo((BodyData)dst);
            }

            public void CopyTopData(int coordinateSource, int coordinateDestination, bool copyClothData, bool copyBodyData)
            {
                if (coordinateSource == coordinateDestination) return;

                var src = GetTopData(coordinateSource);
                var dst = GetTopData(coordinateDestination);
                if (copyClothData)
                    src.CopyTo(dst);
                if (copyBodyData)
                    src.CopyTo((BodyData)dst);
            }

            private Wearing CurrentlyWearing
            {
                get
                {
                    var braIsOnAndEnabled = BraIsOnAndEnabled;
                    var topIsOnAndEnabled = TopIsOnAndEnabled;

                    if (topIsOnAndEnabled)
                        return braIsOnAndEnabled ? Wearing.Both : Wearing.Top;

                    return braIsOnAndEnabled ? Wearing.Bra : Wearing.None;
                }
            }

            private bool BraIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.bra) &&
                                              ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.bra] == 0 &&
                                              CurrentBraData.EnablePushup;

            private bool TopIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.top) &&
                                              ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.top] == 0 &&
                                              CurrentTopData.EnablePushup;

#if KK
            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#elif EC
            //CurrentCoordinateIndex is always zero in EC
            public int CurrentCoordinateIndex => 0;
#endif

            private bool characterLoading;
            public bool CharacterLoading
            {
                get => characterLoading;
                set
                {
                    characterLoading = value;
                    ChaControl.StartCoroutine(Reset());
                    IEnumerator Reset()
                    {
                        yield return null;
                        characterLoading = false;
                    }
                }
            }
        }
    }
}