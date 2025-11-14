using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool descend;
		public bool sprint;
		public bool interact;
		public bool respawn;
		public bool inventory;
		public float scroll;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}
		
		public void OnDescend(InputValue value)
		{
			DescendInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		
		public void OnInteract(InputValue value)
		{
			InteractInput(value.isPressed);
		}
		
		public void OnRespawn(InputValue value)
		{
			RespawnInput(value.isPressed);
		}
		
		public void OnInventory(InputValue value)
		{
			InventoryInput(value.isPressed);
		}
		
		public void OnScroll(InputValue value)
		{
			ScrollInput(value.Get<float>());
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}
		
		public void DescendInput(bool newState)
		{
			descend = newState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		public void InteractInput(bool newSprintState)
		{
			interact = newSprintState;
		}
		
		public void RespawnInput(bool newState)
		{
			respawn = newState;
		}
		
		public void InventoryInput(bool newState)
		{
			inventory = newState;
		}

		public void ScrollInput(float newState)
		{
			scroll = newState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}