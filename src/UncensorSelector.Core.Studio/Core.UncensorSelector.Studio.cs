using KKAPI.Studio;
using KKAPI.Studio.UI;
using System.Collections.Generic;
using System.Linq;
using UniRx;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    public partial class UncensorSelector
    {
        private const string StudioCategoryName = "Uncensor Selector";

        private static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio) return;

            List<string> bodyList = new List<string>();
            List<string> bodyListDisplay = new List<string>();

            foreach (var x in BodyDictionary)
            {
                bodyList.Add(x.Value.BodyGUID);
                bodyListDisplay.Add(x.Value.DisplayName);
            }

            var bodyDropdown = new CurrentStateCategoryDropdown("Uncensor", bodyListDisplay.ToArray(), c => BodyIndex());
            bodyDropdown.Value.Subscribe(value =>
            {
                bool first = true;
                foreach (var controller in StudioAPI.GetSelectedControllers<UncensorSelectorController>())
                {
                    var guid = bodyList[value];

                    //Prevent changing other characters when the value did not actually change
                    if (first && controller.BodyData?.BodyGUID == guid)
                        break;

                    if (controller.BodyData?.BodyGUID != guid)
                        BodyDropdownChangedStudio(controller.ChaControl, guid);

                    first = false;
                }
            });

            int BodyIndex()
            {
                var controller = StudioAPI.GetSelectedControllers<UncensorSelectorController>().First();
                return bodyList.IndexOf(controller.BodyData?.BodyGUID);
            }
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(bodyDropdown);

            List<string> penisList = new List<string>();
            List<string> penisListDisplay = new List<string>();

            foreach (var x in PenisDictionary)
            {
                penisList.Add(x.Value.PenisGUID);
                penisListDisplay.Add(x.Value.DisplayName);
            }

            var penisDropdown = new CurrentStateCategoryDropdown("Penis", penisListDisplay.ToArray(), c => PenisIndex());
            penisDropdown.Value.Subscribe(value =>
            {
                bool first = true;
                foreach (var controller in StudioAPI.GetSelectedControllers<UncensorSelectorController>())
                {
                    var guid = penisList[value];

                    //Prevent changing other characters when the value did not actually change
                    if (first && controller.PenisData?.PenisGUID == guid)
                        break;

                    if (controller.PenisData?.PenisGUID != guid)
                        PenisDropdownChangedStudio(controller.ChaControl, guid);

                    first = false;
                }
            });

            int PenisIndex()
            {
                var controller = StudioAPI.GetSelectedControllers<UncensorSelectorController>().First();
                if (controller.PenisData?.PenisGUID == null)
                    return penisList.IndexOf(DefaultPenisGUID);
                return penisList.IndexOf(controller.PenisData.PenisGUID);
            }
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(penisDropdown);


            List<string> ballsList = new List<string>();
            List<string> ballsListDisplay = new List<string>();

            ballsList.Add("None");
            ballsListDisplay.Add("None");

            foreach (var x in BallsDictionary)
            {
                ballsList.Add(x.Value.BallsGUID);
                ballsListDisplay.Add(x.Value.DisplayName);
            }

            var ballsDropdown = new CurrentStateCategoryDropdown("Balls", ballsListDisplay.ToArray(), c => BallsIndex());
            ballsDropdown.Value.Subscribe(value =>
            {
                bool first = true;
                foreach (var controller in StudioAPI.GetSelectedControllers<UncensorSelectorController>())
                {
                    if (value == 0) //"None"
                    {
                        //Prevent changing other characters when the value did not actually change
                        if (first && controller.DisplayBalls == false)
                            break;

                        if (controller.BallsData?.BallsGUID != null)
                            BallsDropdownChangedStudio(controller.ChaControl, null, false);
                    }
                    else
                    {
                        var guid = ballsList[value];

                        //Prevent changing other characters when the value did not actually change
                        if (first && controller.BallsData?.BallsGUID == guid)
                            break;

                        if (controller.BallsData?.BallsGUID != guid)
                            BallsDropdownChangedStudio(controller.ChaControl, guid, true);
                    }

                    first = false;
                }
            });

            int BallsIndex()
            {
                var controller = StudioAPI.GetSelectedControllers<UncensorSelectorController>().First();
                if (controller.DisplayBalls == false)
                    return ballsList.IndexOf("None");
                if (controller.BallsData?.BallsGUID == null)
                    return ballsList.IndexOf(DefaultBallsGUID);
                return ballsList.IndexOf(controller.BallsData?.BallsGUID);
            }
            StudioAPI.GetOrCreateCurrentStateCategory(StudioCategoryName).AddControl(ballsDropdown);
        }

        //Separate methods so other plugins can hook and reapply their changes if necessary
        private static void BodyDropdownChangedStudio(ChaControl chaControl, string guid)
        {
            var controller = GetController(chaControl);
            controller.BodyGUID = guid;
            controller.UpdateUncensor();
        }
        private static void PenisDropdownChangedStudio(ChaControl chaControl, string guid)
        {
            var controller = GetController(chaControl);
            controller.PenisGUID = guid;
            controller.UpdateUncensor();
        }
        private static void BallsDropdownChangedStudio(ChaControl chaControl, string guid, bool displayBalls)
        {
            var controller = GetController(chaControl);
            controller.BallsGUID = guid;
            controller.DisplayBalls = displayBalls;
            controller.UpdateUncensor();
        }
    }
}
