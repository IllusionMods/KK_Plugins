using KKAPI.Studio;
using KKAPI.Studio.UI;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace KK_Plugins
{
    internal partial class UncensorSelector
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
#if AI || HS2
                    //Hide the body dropdown for male characters
                    if (first)
                        if (controller.ChaControl.sex == 0)
                            bodyDropdown.Visible.OnNext(false);
                        else
                            bodyDropdown.Visible.OnNext(true);

                    if (controller.ChaControl.sex == 0)
                        continue;
#endif

                    var guid = bodyList[value];

                    //Prevent changing other characters when the value did not actually change
                    if (first && controller.BodyData?.BodyGUID == guid)
                        break;

                    first = false;
                    controller.BodyGUID = guid;
                    controller.UpdateUncensor();
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
                foreach (var controller in StudioAPI.GetSelectedControllers<UncensorSelectorController>())
                {
                    var guid = penisList[value];
                    if (controller.PenisData?.PenisGUID != guid)
                    {
                        controller.PenisGUID = guid;
                        controller.UpdateUncensor();
                    }
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
                foreach (var controller in StudioAPI.GetSelectedControllers<UncensorSelectorController>())
                {
                    if (value == 0)//"None"
                    {
                        if (controller.BallsGUID != null)
                        {
                            controller.BallsGUID = null;
                            controller.DisplayBalls = false;
                            controller.UpdateUncensor();
                        }
                    }
                    else
                    {
                        var guid = ballsList[value];
                        if (controller.BallsData?.BallsGUID != guid)
                        {
                            controller.BallsGUID = guid;
                            controller.DisplayBalls = true;
                            controller.UpdateUncensor();
                        }
                    }
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
    }
}
