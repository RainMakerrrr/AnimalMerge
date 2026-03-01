using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;
using MoreMountains.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Framework.Code.UI.Elements
{
    public class TapticButton : MonoBehaviour
    {
        [SerializeField] Sprite tapticOn;
        [SerializeField] Sprite tapticOff;

        Button button;
        Image image;

        IPersistentProgressService progressService;
        ISaveLoadService saveLoadService;

        [Inject]
        void Construct(IPersistentProgressService progressService, ISaveLoadService saveLoadService)
        {
            this.progressService = progressService;
            this.saveLoadService = saveLoadService;
        }

        void OnEnable()
        {
            button = GetComponent<Button>();
            image = GetComponent<Image>();

            MMVibrationManager.SetHapticsActive(progressService.Data.UseTaptic);
            UpdateTapticView();

            button.onClick.AddListener(SwitchTaptic);
        }


        void UpdateTapticView() => image.sprite = progressService.Data.UseTaptic ? tapticOn : tapticOff;

        void SwitchTaptic()
        {
            progressService.Data.UseTaptic = !progressService.Data.UseTaptic;
            saveLoadService.Save(progressService.Data);

            MMVibrationManager.SetHapticsActive(progressService.Data.UseTaptic);

            if (progressService.Data.UseTaptic)
                MMVibrationManager.Haptic(HapticTypes.Selection);

            UpdateTapticView();
        }

        void OnDisable() => button.onClick.RemoveListener(SwitchTaptic);
    }
}