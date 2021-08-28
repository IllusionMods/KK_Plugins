using HSceneUtility;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;

namespace KK_Plugins
{
    public partial class HCharaAdjustment
    {
        public class HCharaAdjustmentController : CharaCustomFunctionController
        {
            private HSceneGuideObject GuideObject;
            private CharacterType ChaType = CharacterType.Unknown;
            private Vector3 OriginalPosition = new Vector3(0, 0, 0);

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }

            /// <summary>
            /// Create a copy of the H scene guide object for each character
            /// </summary>
            internal void CreateGuideObject(HSceneProc hSceneProc, CharacterType characterType)
            {
                ChaType = characterType;
                if (GuideObject == null)
                    GuideObject = Instantiate(hSceneProc.guideObject);
            }

            /// <summary>
            /// Show the guide object associated with this character
            /// </summary>
            public void ShowGuideObject()
            {
                if (GuideObject == null) return;
                if (OriginalPosition == new Vector3(0, 0, 0))
                    SetGuideObjectOriginalPosition();
                GuideObject.gameObject.SetActive(true);

            }
            /// <summary>
            /// Hide the guide object associated with this character
            /// </summary>
            public void HideGuideObject()
            {
                if (GuideObject == null) return;
                GuideObject.gameObject.SetActive(false);
            }

            /// <summary>
            /// Toggle the guide object on and off
            /// </summary>
            public void ToggleGuideObject()
            {
                if (GuideObject == null) return;
                if (GuideObject.gameObject.activeInHierarchy)
                    HideGuideObject();
                else
                    ShowGuideObject();
            }

            /// <summary>
            /// Reset the character's position to the original position before the guide object changed it
            /// </summary>
            public void ResetPosition()
            {
                if (GuideObject == null) return;
                if (OriginalPosition != new Vector3(0, 0, 0))
                {
                    ChaControl.transform.position = OriginalPosition;
                    GuideObject.amount.position = OriginalPosition;
                }
            }

            /// <summary>
            /// Set the original position to the character's position for later use
            /// </summary>
            internal void SetGuideObjectOriginalPosition() => OriginalPosition = ChaControl.transform.position;

            protected override void Update()
            {
                if (GuideObject)
                {
                    //If the guide object is visible set the character position to the guide object position, otherwise the guide object follows the character
                    if (GuideObject.gameObject.activeInHierarchy)
                        ChaControl.transform.position = GuideObject.transform.position;
                    else
                        GuideObject.amount.position = ChaControl.transform.position;

                    if (ChaType == CharacterType.Female1)
                    {
                        if (Female1GuideObject.Value.IsDown())
                            ToggleGuideObject();
                        else if (Female1GuideObjectReset.Value.IsDown())
                            ResetPosition();
                    }
                    else if (ChaType == CharacterType.Female2)
                    {
                        if (Female2GuideObject.Value.IsDown())
                            ToggleGuideObject();
                        else if (Female2GuideObjectReset.Value.IsDown())
                            ResetPosition();
                    }
                    else if (ChaType == CharacterType.Male)
                    {
                        if (MaleGuideObject.Value.IsDown())
                            ToggleGuideObject();
                        else if (MaleGuideObjectReset.Value.IsDown())
                            ResetPosition();
                    }
                }

                base.Update();
            }

            internal enum CharacterType { Female1, Female2, Male, Unknown }
        }
    }
}
