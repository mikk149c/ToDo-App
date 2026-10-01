namespace ToDo_App.UI.Views
{
    /// <summary>
    /// Kanban-style board mockup showing ToDos grouped into columns.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                Child = new TextBlock
                {
                    Style = (Style)Resources["TodoTitle"],
                    Text = $"New ToDo {PlannedItems.Children.Count + 1}",
                },
            };

            PlannedItems.Children.Add(card);
        }
    }
}
