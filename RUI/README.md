# RUI

**RUI** — легковесный компонентный UI-фреймворк для Unity с собственной системой вёрстки, декларативной текстовой разметкой (`.ui`), субпиксельным рендерингом текста и процедурной генерацией геометрии в URP.

Фреймворк создавался вокруг двух ключевых концепций:
1. **Компонентная модель в духе Unity:** узел дерева (`CanvasElement`) сам по себе не имеет визуала и логики; функциональность собирается через композицию компонентов (`CanvasComponent`).
2. **Современный двухпроходный лейаут (Measure & Arrange):** вдохновлен архитектурой WPF, Flutter и Avalonia с чётким разделением расчета геометрии и визуальных трансформаций.

---

## 1. Архитектура: Ноды и Компоненты

В отличие от HTML или Flutter, где тег или виджет объединяет в себе разметку, логику и отрисовку, в RUI ответственность строго разделена:

### `CanvasElement` — структурная нода
Легковесный узел DOM-дерева, не содержащий логики конкретного контрола. Отвечает за:
* Иерархические связи (`parent`, `children`).
* Хранение рассчитанной трансформации (`ResolvedTransform`).
* Слой сортировки (`layerOffset`).
* Двухпроходный цикл лейаута (`Measure` и `Arrange`) и рекурсивный рендер (`RenderTree`).

### `CanvasComponent` — поведение, графика и вёрстка
Функционал узла собирается из независимых компонентов:
* `Image` — отрисовка квадов/спрайтов со скруглением углов или тайлингом.
* `Label` — отображение текста через субпиксельный FreeType или SDF движок.
* `Button` — интерактивность, состояния Hover/Pressed, интеграция с ареной жестов.
* Компоновщики (`*__Composer`) — алгоритмы распределения дочерних узлов.

```text
MyButton {
    Image: color=#2C2C2C borderRadius=4
    Button: normalColor=#00000000 hoverColor=#FFFFFF10 pressedColor=#FFFFFF20
    SizedBox_Composer: size=(120, 32)
}
```

---

## 2. Система компоновки: Measure & Arrange

Вёрстка работает по классическому двухпроходному принципу:
> **Ограничения спускаются вниз. Размеры поднимаются вверх. Родитель задаёт позицию.**  
> *(Constraints go down. Sizes go up. Parent sets position).*

### Координатная система (Y-Up)
В RUI начало координат $(0, 0)$ находится в **левом нижнем углу**:
* Ось **X** направлена **слева направо** ($X = 0$ — левый край, $X = \text{width}$ — правый край).
* Ось **Y** направлена **снизу вверх** ($Y = 0$ — нижний край экрана/контейнера, $Y = \text{height}$ — верхний край).

### Фаза 1: Measure (Снизу вверх)
1. Родитель вызывает `child.Measure(constraints)`, передавая ограничения доступного пространства (`SizeConstraints` / `AxisConstraints`).
2. Дочерний элемент вычисляет свой желаемый размер и кэширует его в `CanvasElement.DesiredSize`.
3. **Приоритет в `CanvasElement.Measure`:**
   * Если на элементе есть компоновщик (`IMeasurable & IComposer`), измерением управляет он.
   * Если компоновщика нет, но **есть дети**, узел опрашивает всех детей и запрашивает максимальный габарит среди них (дефолтный контейнер).
   * И только если детей нет — опрашиваются листовые компоненты вроде `Image` или `Label`. Визуальные компоненты фона не блокируют обход дочерних узлов.

### Фаза 2: Arrange (Сверху вниз)
1. Родитель распределяет слоты для детей и вызывает `child.Arrange(finalRect)`.
2. Позиция `ArrangeRect.pos` **всегда локальна** относительно левого нижнего угла родителя.
3. Элемент фиксирует итоговый `ResolvedTransform = ResolvedTransform.FromRect(rect)`.
4. Если на узле есть `IComposer`, он расставляет своих прямых детей внутри полученного прямоугольника. Если композитора нет — дети растягиваются на полный размер слота `(0, 0, rect.size)`.

---

## 3. Стандартная библиотека компоновщиков

### `Stack_Composer`
Линейный стек элементов (аналог Flexbox / Column / Row).
* **`axis` (`Vertical` / `Horizontal`):**
  * `Vertical`: элементы выстраиваются **сверху вниз** ($Y$ уменьшается от верхнего края окна к низу).
  * `Horizontal`: элементы идут слева направо ($X$ увеличивается).
* **`spacing`:** расстояние между соседними видимыми элементами.
* **`padding`:** внутренние отступы `float4(left, top, right, bottom)`.
* **`fillCross` (`None` / `Child` / `Parent`):**
  * `None`: дети сохраняют свой желаемый размер по поперечной оси.
  * `Child` (Hug): ширина/высота стека подгоняется под самого крупного ребенка, остальные дотягиваются до него.
  * `Parent` (Expand): стек забирает максимум места от родителя, дети растягиваются на всю доступную ширину/высоту.
* **`alignMain` (`Start` / `Center` / `End`):** выравнивание вдоль главной оси (например, `End` для прижатия кнопок вправо — аналог `snap=End`).
* **`alignCross` (`Start` / `Center` / `End`):** выравнивание по поперечной оси. Для горизонтальных строк по умолчанию включен `Center` (автоматическое центрирование иконок и текста разной высоты по вертикали).
* **`reverse`:** инвертирует порядок обхода элементов.

### `Anchor_Composer` и `Anchor`
Многослойное и абсолютное позиционирование (аналог `RectTransform` с анкерами или окна операционной системы).
* Родитель объявляет `Anchor_Composer:`.
* Дочерние узлы задают компонент `Anchor`:
  * `left`, `right` — отступы от левого и правого края.
  * `bottom` — отступ от нижнего края ($Y = 0$).
  * `top` — отступ от верхнего края ($Y = \text{height}$).
  * `width`, `height` — явные размеры.
* Элементы без компонента `Anchor` автоматически занимают 100% пространства родителя (оверлеи, подложки, полноэкранные слои).

### `Fill_Composer`
Контейнер-заполнитель (`Stretch`): принудительно отдает детям все доступное пространство за вычетом отступов `padding`.

### `SizedBox_Composer`
Задание жестких ограничений:
* `size > 0` — навязывает фиксированный размер.
* `size <= 0` — автоматический размер (берется из детей / содержимого).

### `ScrollView`
Скроллируемый контейнер (компонент ввода + компоновщик):
* В `Measure` дает контенту полную свободу по вертикали (`AxisConstraints.Unlimited()`).
* В `Arrange` выставляет контенту его честную полную высоту, а позицию $Y$ смещает согласно `scrollPosition`.
* Автоматически добавляет `Mask` для аппаратного отсечения невидимой части.

---

## 4. Визуальные трансформации: `VisualTransform`

В RUI структура `ResolvedTransform` неизменяема из пользовательского кода — она генерируется фазой `Arrange`. 

Для визуальных трансформаций (поворот стрелочек, масштаб без изменения соседней верстки) используется компонент `VisualTransform`:
```csharp
arrowTransform.angle = model.isCollapsed ? 0f : -90f;
```
Поворот выполняется относительно геометрического центра элемента (`size * 0.5f`) через `Matrix4x4.TRS` поверх готового прямоугольника слота.

---

## 5. Reconciler: Жизненный цикл и AutoWire

### Трёхфазная инициализация дерева
Для исключения проблем с циклическими и опережающими ссылками (*Forward References*, когда элемент сверху ссылается на узел, объявленный ниже в файле разметки), сборка экрана разделена на три фазы:
1. **Reconcile (Build):** рекурсивное создание структуры элементов и инстанцирование компонентов без вызова `OnAttached()`.
2. **PostProcessBindings (Wire):** связывание полей `@Wire` и внедрение зависимостей по всему дереву, когда все ноды экрана уже существуют в памяти.
3. **NotifyAttached (Attach):** вызов `OnAttached()` у всех созданных компонентов.

### Алгоритм поиска зависимостей (`TryResolveComponentDependency`)
Связывание компонентов выполняется по строгим правилам изоляции:
1. **Self:** поиск на самом текущем элементе по типу и ID.
2. **Children (DFS):** поиск вниз по поддереву.
3. **Локальный приоритет:** если на *самом* элементе уже есть компонент нужного типа, используется он, и алгоритм **не идет искать по чужим веткам**.
4. **Upwards & Siblings:** подъем вверх к родителям с проверкой соседних веток дерева.

### Динамический спавн
Метод `reconciler.Spawn(template, parent)`:
* Автоматически добавляет инстанс к родителю.
* Выполняет `Reconcile` и `PostProcessBindings`.
* Сразу активирует `NotifyAttached()`. 
* *Внимание:* повторно вызывать `parent.AddChild(inst)` после `Spawn` нельзя во избежание дублирования узлов в пуле.

---

## 6. Базовые компоненты и контролы

### `Label` и `TextEngine`
* Поддержка субпиксельного LCD-рендеринга через FreeType с кастомным шейдером `SubpixelText` и линейным атласом.
* **Оптическое центрирование базовой линии:** расчет `baselineY` выполняется от высоты заглавных букв (`Cap-Height` $\approx 0.72 \times \text{FontSize}$), что предотвращает сползание текста вниз и вылезание десендеров («р», «у», «д») за границы строки.
* **Pixel Snapping:** координаты центрирования и базовой линии округляются до целых пикселей (`Mathf.Round`), исключая срез макушек букв при точечной фильтрации текстуры.

### `Button`
* Состояния `normalColor`, `hoverColor`, `pressedColor`.
* Интеграция с ареной жестов `GestureArena` через `TapGestureRecognizer`.
* Раздельная обработка: ЛКМ (тап/клик через жест) и ПКМ (`onRightClick` по `PointerDown`).

### `InputField`
* Полноценное поле ввода текста с поддержкой плейсхолдера, фокуса, каретки и горячих клавиш (Backspace, Delete, стрелки, Home, End, Enter, Escape).
* Синглтон фокуса `InputField.ActiveField` (автоматический Unfocus при клике мимо).
* Отрисовка мигающей каретки через стандартный материал поверх текста.
* Поддержка переименования элементов по **F2** (подмена `Label` на `InputField`).

### `DraggableWindow` и `DraggableArea`
* Оконная система со свободным перемещением и масштабированием.
* **Разделение управления:**
  * Перемещение по **СКМ (колесико)** в любой точке окна.
  * Перемещение по **ЛКМ** за выделенный заголовок (`DraggableArea`).
  * Ресайз по **ЛКМ** за любую из 4 граней и 4 углов (зона захвата 8px).
* **Mouse Capture:** удержание перемещения даже при резком вылете мыши за границы окна на высокой скорости.
* **Слои и всплытие:** метод `BringToFront()` поднимает активное окно на передний план.
* **Встраиваемый контент:** метод `window.SetContent<TContent>()` динамически наполняет тело окна любым шаблоном.

### `ContextMenuUI` и `ContextMenusUI`
* Всплывающие контекстные меню, работающие через поверхностный слой `ContextMenus` (`layer=100`).
* Автоматическое закрытие кликом мимо окна через `IsPointerOverHierarchy()`.
* Каскадные подменю с открытием по наведению и итеративным защищенным закрытием (`CloseRoot()` без рекурсивного `StackOverflowException`).

---

## 7. Динамические списки и слоты

### `LotsContainer`
Высокопроизводительный пулинг строк списков:
* Метод `Refresh<TLot, TModel>(models)` удаляет лишние элементы с конца, досоздает недостающие через `reconciler.Spawn` и мгновенно обновляет данные через `lot.Refresh(model)`.
* Обеспечивает строгую синхронизацию количества элементов без дублирования.

### `SlotBuilder` и `SlotReconciler` (`actions.Sync`)
Декларативное добавление полиморфных экшенов в строку (например, иконка глаза `VisibilityToggle`, индикаторы статуса `SimpleIcon`):
```csharp
actions.Sync(s =>
{
    if (model.node.IsVisible.HasValue)
        s.Slot<VisibilityToggle, VisibilityToggle.Model>("eye", ...);

    if (model.node is ISlottable slottable)
        slottable.SyncSlots(s);
});
```
* Сопоставляет слоты по ключу и типу.
* Автоматически удаляет неиспользованные слоты при смене модели.
* Вызывает `reconciler.NotifyAttached()` для созданных элементов слотов.

---

## 8. Рендеринг, URP и стабильность

### Безопасный `UpdateTree`
В фазе обновления (`root.UpdateTree()`) компоненты могут динамически создавать или удалять узлы. Для исключения ошибки `Collection was modified; enumeration operation may not execute` обход списков компонентов и детей выполняется **обратным циклом `for (int i = count - 1; i >= 0; i--)`** (или через `ArrayPool<T>.Shared`), что гарантирует 0 байт GC Alloc и полную безопасность при удалении.

### Хит-тест и интеграция с игровым миром
Методы `CanvasRenderer`:
* `CanvasRenderer.Instance.IsPointerOverUI()` — аналог `IsPointerOverGameObject()`.
* `CanvasRenderer.Instance.Raycast(screenPos)` — возвращает верхний интерактивный узел.
* **Фильтрация контейнеров:** пустые полноэкранные контейнеры без визуала (`MyRoot`, `ContextMenus`) игнорируются хит-тестом и не блокируют клики в 3D-мир.
* **Сортировка по слоям:** элементы с более высоким `layerOffset` опрашиваются первыми независимо от порядка нод в DOM.

### Интеграция с URP (`CustomCanvasFeature`)
Рендеринг выполняется через `ScriptableRenderPass` на событии `AfterRenderingPostProcessing`. Проверка активности `CanvasRenderer` и `cullingMask` камеры исключает нежелательную отрисовку интерфейса, если UI-слой отключен.

### Стабильность разрешения в Unity Editor
`CanvasRenderer` отслеживает изменения размера экрана `float2(Screen.width, Screen.height)` каждый кадр. Это предотвращает искажения верстки при переключении фокуса окон и вызове системных диалогов ОС (например, `EditorUtility.OpenFolderPanel`), временно меняющих высоту Game View.

---

## 9. Примеры разметки (.ui)

### Окно со списком и оверлеем контекстных меню (`WorkspaceScreen.ui`)

```text
MyRoot {
    Anchor_Composer:

    // Окно иерархии слева
    MyHierarchy {
        WorkspaceHierarchyWindow:
        Anchor: left=16 top=48 bottom=38 width=300
        Image: color=#2C2C2C borderRadius=4

        ScrollView:
        {
            Stack_Composer: axis=Vertical fillCross=Parent spacing=0 padding=(0, 3, 0, 3)
            LotsContainer:
        }
    }

    // Статус-бар снизу
    StatusBar {
        Anchor_Composer:
        {
            Anchor: left=0 right=0 bottom=0 height=22
            Image: color=#222222

            Anchor_Composer:
            {
                Anchor: right=12 top=2 bottom=0
                Label: text="Готово" alignment=Right color=#FFFFFF44 fontSize=10
            }
        }
    }

    // Оверлей контекстных меню (находится внизу разметки, поверх всех окон)
    ContextMenus {
        layer=100
        Anchor_Composer:
        ContextMenusUI:
    }
}
```

### Строка иерархии с кнопками, иконками, текстом и инпутом переименования (`HierarchyLot.ui`)

```text
template Lot {
    HierarchyLot:
    SizedBox_Composer: size=(0, 16)
    Image#bgImage: color=#00000000 borderRadius=4
    Button#bodyButton: normalColor=#00000000 hoverColor=#FFFFFF10 pressedColor=#FFFFFF20

    {
        Fill_Composer: padding=(3, 0, 3, 0)

        // Левая группа (отступы вложенности, стрелочка, иконка, имя)
        LeftGroup {
            Stack_Composer: axis=Horizontal alignCross=Center spacing=4

            {
                SizedBox_Composer#depthBox: size=(0, 16)
                Image#depthIcon: sprite="depth-tiling" color=#5A5A5A mode=Tiling
            }

            {
                VisualTransform#arrowTransform:
                SizedBox_Composer: size=(16, 16)
                Image#arrow: sprite="arrow"
                Button#arrowButton: normalColor=#FFFFFF hoverColor=#EEEEEE pressedColor=#CCCCCC
            }

            {
                SizedBox_Composer: size=(16, 16)
                Image#mainIcon:
            }

            // Обычный текст узла
            {
                Label#text: fontSize=10 color=#FFFFFF alignment=Left
            }

            // Поле переименования (активируется по F2)
            RenameBox {
                SizedBox_Composer: size=(130, 16)
                Image: color=#141414 borderRadius=2
                InputField#renameInput: placeholder="Имя узла..."
                Label#renameText: fontSize=10 color=#FFFFFF alignment=Left
            }
        }

        // Правая группа (динамические слоты действий: глазик, индикаторы)
        actions {
            Stack_Composer: axis=Horizontal alignMain=End spacing=4 padding=(0, 0, 6, 0)
        }
    }
}
```

### Перемещаемое окно с заголовком и динамическим телом (`DraggableWindow.ui`)

```text
template DraggableWindow {
    DraggableWindow:
    Anchor: left=120 bottom=120 width=280 height=180
    Image: color=#1A1A1AF5 borderRadius=6
    Anchor_Composer:

    // Хедер окна: перемещение по ЛКМ
    Header {
        Anchor: left=0 right=0 top=0 height=26
        Image: color=#262626 borderRadius=(6, 6, 0, 0)
        DraggableArea:

        Stack_Composer: axis=Horizontal alignCross=Center spacing=6 padding=(10, 0, 10, 0)
        {
            Label#titleLabel: text="Окно" fontSize=11 color=#FFFFFF alignment=Left
        }
    }

    // Тело окна: динамический контент через window.SetContent<T>()
    Body#body {
        Anchor: left=0 right=0 top=26 bottom=0
        Fill_Composer: padding=(10, 10, 10, 10)
    }
}
```