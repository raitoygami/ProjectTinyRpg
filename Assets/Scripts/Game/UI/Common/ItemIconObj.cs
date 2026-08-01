using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ItemIconObj : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler,
    
    IPointerClickHandler, 
    IBeginDragHandler,              // 新增：开始拖拽
    IDragHandler,                   // 新增：拖拽中
    IEndDragHandler                 // 新增：拖拽结束
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _amount;
    private AsyncOperationHandle<Sprite> _iconHandle; // 关键：保存 handle
    private IItemIconOwner _Owner;
    private ItemStack _itemStack;

    public IItemIconOwner GetOwner()
    {
        return _Owner;
    }

    public void SetOwner(IItemIconOwner owner)
    {
        _Owner = owner;
    }

    public ItemStack GetItemStack()
    {
        return  _itemStack;
    }
    
    public void SetItemStack(ItemStack itemStack)
    {
        if (_itemStack != itemStack)
        {
            // 安全释放
            if (_iconHandle.IsValid())
            {
                Addressables.Release(_iconHandle);
                _iconHandle = default; // 可选：清空
            }

            // 图标
            _iconHandle = Addressables.LoadAssetAsync<Sprite>(itemStack.GetIconAddressable());

            _iconHandle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                    _icon.sprite = operationHandle.Result;
                else
                    Debug.LogError($"加载图标失败: {itemStack.GetIconAddressable()}");
            };
        }

        // 数量
        _amount.text = itemStack.Count.ToString();
        _amount.gameObject.SetActive(itemStack.Stackable());

        _itemStack = itemStack;
    }

    private Coroutine _showCoroutine;
    private const float _showTipDelay = 0.3f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_itemStack == null) return;
        // 取消任何隐藏动作（如果之前触发了隐藏）
        // 启动延迟显示
        if (_showCoroutine != null) StopCoroutine(_showCoroutine);
        _showCoroutine = StartCoroutine(ShowTooltipAfterDelay(eventData));
    }

    private IEnumerator ShowTooltipAfterDelay(PointerEventData eventData)
    {
        yield return new WaitForSeconds(_showTipDelay);
        // 显示时获取最新的位置
        var pos = GetTooltipPosition(eventData);
        UIRoot.Instance.ToolTipUI.ShowTip(_itemStack, pos);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        UIRoot.Instance.ToolTipUI?.HideTip();
    }

    /// <summary>
    /// 针对 Screen Space - Camera Canvas 优化的提示框位置计算
    /// </summary>
    private Vector2 GetTooltipPosition(PointerEventData eventData)
    {
        var canvasRect = UIRoot.Instance.ToolTipUI.GetComponent<RectTransform>();

        if (canvasRect == null)
            return eventData.position; // 兜底

        // 1. 获取 UI 相机
        var uiCamera = UIRoot.Instance.GetUICamera();

        // 2. 将 Icon 的世界坐标转为屏幕像素坐标（必须用 UI 相机）
        var screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, _icon.rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out var localPos);

        // 可根据需要微调偏移
        var size = GetComponent<RectTransform>().sizeDelta;
        return localPos - new Vector2(size.x * 0.5f, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        PhysicsUtil.SetRaycastTargetRecursively(gameObject, false);
        UIRoot.Instance.ToolTipUI?.HideTip();
        DragManager.Instance.OnBeginDrag(eventData, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DragManager.Instance.OnDrag(eventData, this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragManager.Instance.OnEndDrag(eventData, this);
        PhysicsUtil.SetRaycastTargetRecursively(gameObject, true);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 如果是装备更新则更新表现
            if (_Owner.OnMouseRightClick(eventData, this))
            {
                if (_itemStack.IsEquip())
                {
                    this.PublishGlobal(Context.EquipmentUpdate);
                }
            }
        }
    }

    public void Discard()
    {
        _Owner?.Discard(this);
    }

    public bool RemoveFromOwner()
    {
        return _Owner.TryRemove(this);
    }

    public void Restore()
    {
        _Owner.Restore(this);
    }
    
}