using Rubber.Gameplay.Ducks;
using UnityEngine;
namespace Rubber.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(VillaDuckBuoyancy),typeof(RubberDuckInteractable))]
    public sealed class VillaDuckReturn : MonoBehaviour
    {
        public RubberDuckReturnRegistry registry;
        VillaDuckBuoyancy buoyancy;
        RubberDuckInteractable duck;
        void OnEnable()
        {
            buoyancy=GetComponent<VillaDuckBuoyancy>();duck=GetComponent<RubberDuckInteractable>();
            buoyancy.onSettled.AddListener(Register);
        }
        void OnDisable(){if(buoyancy)buoyancy.onSettled.RemoveListener(Register);}
        void Register()
        {
            if(registry && !duck.IsHeld && buoyancy.IsSettled && buoyancy.water &&
               buoyancy.water.Contains(duck.GetComponent<Rigidbody>().worldCenterOfMass))registry.TryRegister(duck);
        }
    }
}
