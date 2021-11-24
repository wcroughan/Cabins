// GENERATED AUTOMATICALLY FROM 'Assets/PlayerControl/InputActions.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @InputActions : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputActions"",
    ""maps"": [
        {
            ""name"": ""WorldMovement"",
            ""id"": ""2593a7a2-cf10-4e5f-88e9-761a6f3e832a"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""be056074-ded0-44dc-a474-ab39a22e6965"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""dc551ff3-b4f4-4298-9cb9-261219b85dae"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Crouch"",
                    ""type"": ""Button"",
                    ""id"": ""e60f76fe-ce2d-4ca1-a0fa-fb08dcf68432"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Sprint"",
                    ""type"": ""Button"",
                    ""id"": ""72286099-7922-433e-96da-279d20612303"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleFlying"",
                    ""type"": ""Button"",
                    ""id"": ""12741032-8e35-4bd3-8254-8f485f0a281a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""7002be91-1924-44c2-afb5-da37750c26ce"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""f7b2c36b-2d75-4e44-a35f-0c948f87c79e"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""184fb30d-afda-480a-b92f-19c4c27c67c4"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""419b0825-9adc-4e7d-a977-07f962a6b16e"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""8d9a0eae-6850-40d9-ba2f-c04b2b547682"",
                    ""path"": ""<Keyboard>/f"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""646d2969-03d8-40df-aac0-dd88aea5d350"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""54d7821d-1da0-40db-a5c8-cefde3aaecd4"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Crouch"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d9c7012e-cd9d-4408-84b8-984fcec04323"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Sprint"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a306e459-29eb-42f0-95c0-0ce75391fbe3"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleFlying"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""GameControl"",
            ""id"": ""f084751a-1818-492a-8e31-d1df70721bc9"",
            ""actions"": [
                {
                    ""name"": ""Pause"",
                    ""type"": ""Button"",
                    ""id"": ""2381bab2-f612-466a-ad5e-426cc6a4a59d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9bc1e7ef-ffba-44e0-bc91-1d2ebaab45db"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Pause"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // WorldMovement
        m_WorldMovement = asset.FindActionMap("WorldMovement", throwIfNotFound: true);
        m_WorldMovement_Move = m_WorldMovement.FindAction("Move", throwIfNotFound: true);
        m_WorldMovement_Jump = m_WorldMovement.FindAction("Jump", throwIfNotFound: true);
        m_WorldMovement_Crouch = m_WorldMovement.FindAction("Crouch", throwIfNotFound: true);
        m_WorldMovement_Sprint = m_WorldMovement.FindAction("Sprint", throwIfNotFound: true);
        m_WorldMovement_ToggleFlying = m_WorldMovement.FindAction("ToggleFlying", throwIfNotFound: true);
        // GameControl
        m_GameControl = asset.FindActionMap("GameControl", throwIfNotFound: true);
        m_GameControl_Pause = m_GameControl.FindAction("Pause", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // WorldMovement
    private readonly InputActionMap m_WorldMovement;
    private IWorldMovementActions m_WorldMovementActionsCallbackInterface;
    private readonly InputAction m_WorldMovement_Move;
    private readonly InputAction m_WorldMovement_Jump;
    private readonly InputAction m_WorldMovement_Crouch;
    private readonly InputAction m_WorldMovement_Sprint;
    private readonly InputAction m_WorldMovement_ToggleFlying;
    public struct WorldMovementActions
    {
        private @InputActions m_Wrapper;
        public WorldMovementActions(@InputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_WorldMovement_Move;
        public InputAction @Jump => m_Wrapper.m_WorldMovement_Jump;
        public InputAction @Crouch => m_Wrapper.m_WorldMovement_Crouch;
        public InputAction @Sprint => m_Wrapper.m_WorldMovement_Sprint;
        public InputAction @ToggleFlying => m_Wrapper.m_WorldMovement_ToggleFlying;
        public InputActionMap Get() { return m_Wrapper.m_WorldMovement; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(WorldMovementActions set) { return set.Get(); }
        public void SetCallbacks(IWorldMovementActions instance)
        {
            if (m_Wrapper.m_WorldMovementActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnMove;
                @Jump.started -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnJump;
                @Jump.performed -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnJump;
                @Jump.canceled -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnJump;
                @Crouch.started -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnCrouch;
                @Crouch.performed -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnCrouch;
                @Crouch.canceled -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnCrouch;
                @Sprint.started -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnSprint;
                @Sprint.performed -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnSprint;
                @Sprint.canceled -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnSprint;
                @ToggleFlying.started -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnToggleFlying;
                @ToggleFlying.performed -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnToggleFlying;
                @ToggleFlying.canceled -= m_Wrapper.m_WorldMovementActionsCallbackInterface.OnToggleFlying;
            }
            m_Wrapper.m_WorldMovementActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Jump.started += instance.OnJump;
                @Jump.performed += instance.OnJump;
                @Jump.canceled += instance.OnJump;
                @Crouch.started += instance.OnCrouch;
                @Crouch.performed += instance.OnCrouch;
                @Crouch.canceled += instance.OnCrouch;
                @Sprint.started += instance.OnSprint;
                @Sprint.performed += instance.OnSprint;
                @Sprint.canceled += instance.OnSprint;
                @ToggleFlying.started += instance.OnToggleFlying;
                @ToggleFlying.performed += instance.OnToggleFlying;
                @ToggleFlying.canceled += instance.OnToggleFlying;
            }
        }
    }
    public WorldMovementActions @WorldMovement => new WorldMovementActions(this);

    // GameControl
    private readonly InputActionMap m_GameControl;
    private IGameControlActions m_GameControlActionsCallbackInterface;
    private readonly InputAction m_GameControl_Pause;
    public struct GameControlActions
    {
        private @InputActions m_Wrapper;
        public GameControlActions(@InputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Pause => m_Wrapper.m_GameControl_Pause;
        public InputActionMap Get() { return m_Wrapper.m_GameControl; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameControlActions set) { return set.Get(); }
        public void SetCallbacks(IGameControlActions instance)
        {
            if (m_Wrapper.m_GameControlActionsCallbackInterface != null)
            {
                @Pause.started -= m_Wrapper.m_GameControlActionsCallbackInterface.OnPause;
                @Pause.performed -= m_Wrapper.m_GameControlActionsCallbackInterface.OnPause;
                @Pause.canceled -= m_Wrapper.m_GameControlActionsCallbackInterface.OnPause;
            }
            m_Wrapper.m_GameControlActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Pause.started += instance.OnPause;
                @Pause.performed += instance.OnPause;
                @Pause.canceled += instance.OnPause;
            }
        }
    }
    public GameControlActions @GameControl => new GameControlActions(this);
    public interface IWorldMovementActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnCrouch(InputAction.CallbackContext context);
        void OnSprint(InputAction.CallbackContext context);
        void OnToggleFlying(InputAction.CallbackContext context);
    }
    public interface IGameControlActions
    {
        void OnPause(InputAction.CallbackContext context);
    }
}
