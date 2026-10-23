namespace ToDo_App.UI.Board;

/// <summary>
/// Grays out what's waiting for the server. An element says which board Ids it shows with <see cref="SetIds"/>, and
/// is grayed out and locked while any of them is pending, whatever kind of element it is. An element built while its
/// Id is pending is grayed out straight away.
/// </summary>
/// <remarks>One set of pending Ids for the app's one window. Used on the UI thread only.</remarks>
public static class Pending
{
    private static readonly HashSet<Guid> pendingIds = new();
    private static readonly List<WeakReference<UIElement>> marked = new();

    public static readonly DependencyProperty IdsProperty = DependencyProperty.RegisterAttached(
        "Ids", typeof(Guid[]), typeof(Pending), new PropertyMetadata(null, onIdsChanged));

    /// <summary>The columns, todos or tags <paramref name="element"/> shows.</summary>
    public static void SetIds(UIElement element, params Guid[] ids) => element.SetValue(IdsProperty, ids);

    public static void Add(IEnumerable<Guid> ids)
    {
        int before = pendingIds.Count;
        pendingIds.UnionWith(ids);
        if (pendingIds.Count != before)
        {
            restyleAll();
        }
    }

    public static void Remove(IEnumerable<Guid> ids)
    {
        int before = pendingIds.Count;
        pendingIds.ExceptWith(ids);
        if (pendingIds.Count != before)
        {
            restyleAll();
        }
    }

    /// <summary>Nothing is pending any more, for example when the app switches to another board.</summary>
    public static void Clear() => Remove(pendingIds.ToList());

    private static void onIdsChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        marked.Add(new WeakReference<UIElement>((UIElement)element));
        restyle((UIElement)element);
    }

    private static void restyle(UIElement element)
    {
        bool isPending = element.GetValue(IdsProperty) is Guid[] ids && ids.Any(pendingIds.Contains);
        element.Opacity = isPending ? 0.5 : 1;
        element.IsHitTestVisible = !isPending;
    }

    private static void restyleAll()
    {
        // Elements the page has thrown away are gone once nothing else holds them
        marked.RemoveAll(reference => !reference.TryGetTarget(out _));
        foreach (WeakReference<UIElement> reference in marked)
        {
            if (reference.TryGetTarget(out UIElement? element))
            {
                restyle(element);
            }
        }
    }
}
