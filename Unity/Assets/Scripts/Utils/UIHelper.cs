using System.Collections.Generic;
using System;
using UnityEngine;
using Zenject;
using System.Linq;
using Object = UnityEngine.Object;

namespace InGame
{
    public class UIHelper
    {
        [Inject] private DiContainer container;

        [HideInCallstack]
        public void Refresh<TUILot, TModel>(Transform parent, TUILot prefab, IEnumerable<TModel> models, params object[] toInject) where TUILot : UILot<TModel>
        {
            // Remove children without TUILot
            int childCount = parent.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent<TUILot>() == null)
                {
                    GameObject.DestroyImmediate(child.gameObject);
                    childCount--;
                }
            }

            int diff = models.Count() - parent.childCount;

            DiContainer sub = container.CreateSubContainer();
            sub.BindInstances(toInject);

            // Add new lots if needed
            for (int i = 0; i < diff; i++)
            {
                sub.InstantiatePrefab(prefab, parent);
            }

            // Remove lots if needed
            for (int i = 0; i < -diff; i++)
            {
                GameObject.DestroyImmediate(parent.GetChild(0).gameObject);
            }

            int index = 0;
            foreach (TModel model in models)
            {
                Transform child = parent.GetChild(index);
                TUILot lot = child.GetComponent<TUILot>();
                index++;

                if (lot == null)
                {
                    Debug.LogError($"Failed to Refresh due to child GameObject '{child.name}' of parent '{parent.name}' does not have {typeof(TUILot)} component.", parent);
                    return;
                }
                lot.Refresh(model);
            }
        }
        public IEnumerable<TModel> GetModels<TModel>(Transform parent)
        {
            foreach (Transform child in parent)
            {
                yield return child.GetComponent<UILot<TModel>>().ActiveModel;
            }
        }
        public IEnumerable<TLot> GetLots<TLot, TModel>(Transform parent) where TLot : UILot<TModel>
        {
            foreach (Transform child in parent)
            {
                yield return child.GetComponent<TLot>();
            }
        }
        public void FillWithInstance<TModel, TInstance>(Transform parent, IEnumerable<TModel> models, TInstance activeInstance, Action<TInstance, TModel> refreshFunc) where TInstance : MonoBehaviour
        {
            activeInstance.gameObject.SetActive(true);

            foreach (TModel model in models)
            {
                GameObject inst = container.InstantiatePrefab(activeInstance.gameObject, parent);
                refreshFunc(inst.GetComponent<TInstance>(), model);
            }

            activeInstance.gameObject.SetActive(false);
        }
        public void AppendWithInstance<TModel, TInstance>(Transform parent, TModel model, TInstance activeInstance, Action<TInstance, TModel> refreshFunc) where TInstance : MonoBehaviour
        {
            activeInstance.gameObject.SetActive(true);

            GameObject inst = container.InstantiatePrefab(activeInstance.gameObject, parent);
            refreshFunc(inst.GetComponent<TInstance>(), model);

            activeInstance.gameObject.SetActive(false);
        }
        public void AppendAsLast<TUILot, TModel>(Transform parent, TUILot prefab, TModel model) where TUILot : UILot<TModel>
        {
            GameObject inst = container.InstantiatePrefab(prefab, parent);
            inst.GetComponent<TUILot>().Refresh(model);

            inst.transform.SetAsLastSibling();
        }

        public void FillBySelector<TModel, TLot>(Transform parent, IEnumerable<TModel> models, Func<TModel, TLot> prefabByModel) where TLot : UILot<TModel>
        {
            ClearChildren(parent);

            DiContainer sub = container.CreateSubContainer();

            foreach (TModel model in models)
            {
                TLot prefab = prefabByModel(model);
                TLot instance = sub.InstantiatePrefab(prefab, parent).GetComponent<TLot>();
                instance.Refresh(model);
            }
        }
        public void FillBySelector(Transform parent, IEnumerable<object> models, Func<object, KeyValuePair<GameObject, object>> kvByModel)
        {
            ClearChildren(parent);

            foreach (object model in models)
            {
                KeyValuePair<GameObject, object> kv = kvByModel(model);

                DiContainer sub = container.CreateSubContainer();
                if (kv.Value != null) sub.Bind(kv.Value.GetType()).FromInstance(kv.Value);

                GameObject instance = sub.InstantiatePrefab(kv.Key, parent);
                instance.GetComponent<IUILot>().OnConstruct();
            }
        }

        public void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        [HideInCallstack]
        public bool KeepCount<T>(GameObject prefab, Transform container, int targetCount, List<T> list, params object[] toInject)
        {
            int count = container.childCount;
            bool isDirty = count != targetCount;

            while (count > targetCount)
            {
                Object.Destroy(container.GetChild(count - 1).gameObject);
                list.RemoveAt(count - 1);
                count--;
            }

            while (count < targetCount)
            {
                list.Add(this.container.InstantiatePrefabForComponent<T>(prefab, container, toInject));
                count++;
            }

            return isDirty;
        }
    }
}