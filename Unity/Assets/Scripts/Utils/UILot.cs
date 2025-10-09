using UnityEngine;
using Zenject;

public interface IUILot
{
    void OnConstruct();
}
public abstract class UILot<T> : MonoBehaviour, IUILot
{
    public T ActiveModel => model;

    [InjectOptional] protected T model;

    protected bool isRefreshing;

    public void Refresh(T model)
    {
        this.model = model;

        isRefreshing = true;
        Refresh();
        isRefreshing = false;
    }

    public void OnConstruct()
    {
        isRefreshing = true;
        Refresh();
        isRefreshing = false;
    }
    protected abstract void Refresh();
}