using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpineAnimationDriver))]
public class SpineInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriver;

    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private InputActionProperty attackAction;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float movingThreshold = 0.05f;
    [SerializeField] private bool flipByMoveX = true;

    private InputAction _runtimeMoveAction;
    private InputAction _runtimeAttackAction;

    private void Awake()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        EnsureActions();
    }

    private void OnEnable()
    {
        InputAction move = GetMoveAction();
        if (move != null)
            move.Enable();

        InputAction attack = GetAttackAction();
        if (attack != null)
        {
            attack.performed += OnAttackPerformed;
            attack.Enable();
        }
    }

    private void OnDisable()
    {
        InputAction attack = GetAttackAction();
        if (attack != null)
            attack.performed -= OnAttackPerformed;
    }

    private void Update()
    {
        if (animationDriver == null) return;

        Vector2 move = Vector2.zero;
        InputAction action = GetMoveAction();
        if (action != null)
            move = action.ReadValue<Vector2>();

        bool isMoving = move.sqrMagnitude > movingThreshold * movingThreshold;
        animationDriver.SetMoving(isMoving);

        if (isMoving)
        {
            Vector3 delta = new Vector3(move.x, move.y, 0f) * (moveSpeed * Time.deltaTime);
            transform.position += delta;

            if (flipByMoveX && Mathf.Abs(move.x) > 0.001f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(move.x);
                transform.localScale = scale;
            }
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext _)
    {
        if (animationDriver == null) return;
        animationDriver.PlayAttack();
    }

    private void EnsureActions()
    {
        // 씬에 "빈 InputAction"이 직렬화된 경우(바인딩 0개) 기본 바인딩으로 폴백합니다.
        if (!HasUsableBindings(moveAction.action))
            _runtimeMoveAction = CreateDefaultMoveAction();

        if (!HasUsableBindings(attackAction.action))
            _runtimeAttackAction = CreateDefaultAttackAction();
    }

    private InputAction GetMoveAction()
    {
        return HasUsableBindings(moveAction.action) ? moveAction.action : _runtimeMoveAction;
    }

    private InputAction GetAttackAction()
    {
        return HasUsableBindings(attackAction.action) ? attackAction.action : _runtimeAttackAction;
    }

    private static bool HasUsableBindings(InputAction action)
    {
        if (action == null) return false;
        // InputAction.bindings는 null이 아닌 값 타입(읽기 전용 배열)이라 null 체크가 컴파일 에러를 유발할 수 있습니다.
        return action.bindings.Count > 0;
    }

    private static InputAction CreateDefaultMoveAction()
    {
        var action = new InputAction(name: "Move", type: InputActionType.Value);
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        action.AddBinding("<Gamepad>/leftStick");
        return action;
    }

    private static InputAction CreateDefaultAttackAction()
    {
        var action = new InputAction(name: "Attack", type: InputActionType.Button);
        action.AddBinding("<Keyboard>/j");
        action.AddBinding("<Mouse>/leftButton");
        action.AddBinding("<Gamepad>/buttonSouth");
        return action;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();
    }
#endif
}
