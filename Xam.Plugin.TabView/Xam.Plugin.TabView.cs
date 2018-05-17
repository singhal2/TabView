﻿using CarouselView.FormsPlugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Xamarin.Forms;

namespace Xam.Plugin.TabView
{
    public delegate void PositionChangingEventHandler(object sender, PositionChangingEventArgs e);
    public delegate void PositionChangedEventHandler(object sender, PositionChangedEventArgs e);

    public class PositionChangingEventArgs : EventArgs
    {
        public bool Canceled { get; set; }
        public int NewPosition { get; set; }
        public int OldPosition { get; set; }
    }

    public class PositionChangedEventArgs : EventArgs
    {
        public int NewPosition { get; set; }
        public int OldPosition { get; set; }
    }

    public class TabViewControl : ContentView
    {
        private StackLayout _mainContainerSL;
        private Grid _headerContainerGrid;
        private CarouselViewControl _carouselView;

        public ObservableCollection<TabItem> ItemSource { get; set; }

        public event PositionChangingEventHandler PositionChanging;
        public event PositionChangedEventHandler PositionChanged;

        protected virtual void OnPositionChanging(ref PositionChangingEventArgs e)
        {
            PositionChangingEventHandler handler = PositionChanging;
            handler?.Invoke(this, e);
        }

        protected virtual void OnPositionChanged(PositionChangedEventArgs e)
        {
            PositionChangedEventHandler handler = PositionChanged;
            handler?.Invoke(this, e);
        }

        public TabViewControl()
        {
            //Parameterless constructor required for xaml instantiation.
        }

        public TabViewControl(IList<TabItem> tabItems, int selectedTabIndex = 0)
        {
            Initialize(tabItems, selectedTabIndex);
        }

        private void Initialize(IList<TabItem> tabItems, int selectedTabIndex = 0)
        {
            ItemSource = new ObservableCollection<TabItem>();
            void setupTab(TabItem tab)
            {
                tab.HeaderTextColor = HeaderTabTextColor;
                tab.HeaderSelectionUnderlineColor = HeaderSelectionUnderlineColor;
                tab.HeaderSelectionUnderlineThickness = HeaderSelectionUnderlineThickness;
                if (tab.HeaderSelectionUnderlineWidth > 0)
                {
                    tab.HeaderSelectionUnderlineWidth = HeaderSelectionUnderlineWidth;
                }
                tab.HeaderTabTextFontSize = HeaderTabTextFontSize;
                tab.HeaderTabTextFontFamily = HeaderTabTextFontFamily;
                tab.HeaderTabTextFontAttributes = HeaderTabTextFontAttributes;
            }
            foreach (var tab in tabItems)
            {
                
                setupTab(tab);
                ItemSource.Add(tab);
            }

            Init();

            ItemSource.CollectionChanged += (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
            {
                foreach (var tab in ItemSource)
                {
                    setupTab(tab);
                }
                InitTabs();
            };

            InitTabs();
            SetPosition(selectedTabIndex, true);

            _carouselView.PropertyChanged += _carouselView_PropertyChanged;
        }

        private bool _supressCarouselViewPositionChangedEvent = false;
        private void _carouselView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_carouselView.Position) && !_supressCarouselViewPositionChangedEvent)
            {
                var positionChangingArgs = new PositionChangingEventArgs()
                {
                    Canceled = false,
                    NewPosition = _carouselView.Position,
                    OldPosition = SelectedTabIndex
                };

                OnPositionChanging(ref positionChangingArgs);

                if (positionChangingArgs != null && positionChangingArgs.Canceled)
                {
                    _supressCarouselViewPositionChangedEvent = true;
                    _carouselView.PositionSelected -= _carouselView_PositionSelected;
                    _carouselView.PropertyChanged -= _carouselView_PropertyChanged;
                    _carouselView.Position = SelectedTabIndex;
                    _carouselView.PositionSelected += _carouselView_PositionSelected;
                    _carouselView.PropertyChanged += _carouselView_PropertyChanged;
                    _supressCarouselViewPositionChangedEvent = false;
                }
            }
        }

        private void Init()
        {
            _headerContainerGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.Start,
                BackgroundColor = HeaderBackgroundColor,
                MinimumHeightRequest = 50
            };

            _carouselView = new CarouselViewControl
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = ContentHeight,
                ShowArrows = false,
                ShowIndicators = false,
                BindingContext = this
            };

            _mainContainerSL = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = { _headerContainerGrid, _carouselView },
                Spacing = 0
            };

            Content = _mainContainerSL;
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext != null)
            {
                foreach (var tab in ItemSource)
                {
                    if (tab is TabItem view)
                    {
                        view.Content.BindingContext = BindingContext;
                    }
                }
            }
        }

        private void _carouselView_PositionSelected(object sender, PositionSelectedEventArgs e)
        {
            SetPosition(e.NewValue);
        }

        private void InitTabs()
        {
            _headerContainerGrid.Children.Clear();
            _headerContainerGrid.ColumnDefinitions.Clear();
            _headerContainerGrid.RowDefinitions.Clear();

            var tabSize = (TabSizeOption.IsAbsolute && TabSizeOption.Value.Equals(0)) ? new GridLength(1, GridUnitType.Star) : TabSizeOption;

            for (int i = 0; i < ItemSource.Count; i++)
            {
                _headerContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = tabSize });

                var tab = ItemSource[i];
                tab.IsCurrent = i == SelectedTabIndex;

                var headerLabel = new Label
                {
                    Margin = new Thickness(5, 10, 5, 0),
                    BindingContext = tab,
                    VerticalTextAlignment = TextAlignment.Start,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.Center
                };
                headerLabel.SetBinding(Label.TextProperty, nameof(TabItem.HeaderText));
                headerLabel.SetBinding(Label.TextColorProperty, nameof(TabItem.HeaderTextColor));
                headerLabel.SetBinding(Label.FontSizeProperty, nameof(TabItem.HeaderTabTextFontSize));
                headerLabel.SetBinding(Label.FontFamilyProperty, nameof(TabItem.HeaderTabTextFontFamily));
                headerLabel.SetBinding(Label.FontAttributesProperty, nameof(TabItem.HeaderTabTextFontAttributes));

                var selectionBarBoxView = new BoxView
                {
                    VerticalOptions = LayoutOptions.EndAndExpand,
                    BindingContext = tab,
                    HeightRequest = HeaderSelectionUnderlineThickness,
                    WidthRequest = HeaderSelectionUnderlineWidth
                };
                selectionBarBoxView.HorizontalOptions = tab.HeaderSelectionUnderlineWidth > 0 ? LayoutOptions.CenterAndExpand : LayoutOptions.FillAndExpand;
                selectionBarBoxView.SetBinding(BoxView.IsVisibleProperty, nameof(TabItem.IsCurrent));
                selectionBarBoxView.SetBinding(BoxView.ColorProperty, nameof(TabItem.HeaderSelectionUnderlineColor));
                selectionBarBoxView.SetBinding(BoxView.WidthRequestProperty, nameof(TabItem.HeaderSelectionUnderlineWidth));
                selectionBarBoxView.SetBinding(BoxView.HeightRequestProperty, nameof(TabItem.HeaderSelectionUnderlineThickness));

                selectionBarBoxView.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                {
                    if (e.PropertyName == nameof(TabItem.IsCurrent))
                    {
                        SetPosition(ItemSource.IndexOf((TabItem)((BoxView)sender).BindingContext));
                    }
                    if (e.PropertyName == nameof(WidthRequest))
                    {
                        selectionBarBoxView.HorizontalOptions = tab.HeaderSelectionUnderlineWidth > 0 ? LayoutOptions.CenterAndExpand : LayoutOptions.FillAndExpand;
                    }
                };

                var headerItemSL = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children = { headerLabel, selectionBarBoxView }
                };

                var tapRecognizer = new TapGestureRecognizer();
                int capturedIndex = i;
                tapRecognizer.Tapped += (object s, EventArgs e) =>
                {
                    _supressCarouselViewPositionChangedEvent = true;
                    SetPosition(capturedIndex);
                    _supressCarouselViewPositionChangedEvent = false;
                };
                headerItemSL.GestureRecognizers.Add(tapRecognizer);

                _headerContainerGrid.Children.Add(headerItemSL, i, 0);
            }

            _carouselView.ItemsSource = ItemSource.Select(t => t.Content);
        }

        #region HeaderBackgroundColor
        public Color HeaderBackgroundColor
        {
            get { return (Color)GetValue(HeaderBackgroundColorProperty); }
            set { SetValue(HeaderBackgroundColorProperty, value); }
        }
        private static void HeaderBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl)
            {
                tabViewControl._headerContainerGrid.BackgroundColor = (Color)newValue;
            }
        }
        public readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(nameof(HeaderBackgroundColor), typeof(Color), typeof(TabViewControl), Color.Black, BindingMode.Default, null, HeaderBackgroundColorChanged);
        #endregion

        #region HeaderTabTextColor
        public Color HeaderTabTextColor
        {
            get { return (Color)GetValue(HeaderTabTextColorProperty); }
            set { SetValue(HeaderTabTextColorProperty, value); }
        }
        private static void HeaderTabTextColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderTextColor = (Color)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderTabTextColorProperty =
            BindableProperty.Create(nameof(HeaderTabTextColor), typeof(Color), typeof(TabViewControl), Color.White, BindingMode.OneWay, null, HeaderTabTextColorChanged);
        #endregion

        #region ContentHeight
        public double ContentHeight
        {
            get { return (double)GetValue(ContentHeightProperty); }
            set { SetValue(ContentHeightProperty, value); }
        }
        private static void ContentHeightChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl._carouselView != null)
            {
                tabViewControl._carouselView.HeightRequest = (double)newValue;
            }
        }
        public readonly BindableProperty ContentHeightProperty = BindableProperty.Create(nameof(ContentHeight), typeof(double), typeof(TabViewControl), (double)200, BindingMode.Default, null, ContentHeightChanged);
        #endregion

        #region HeaderSelectionUnderlineColor
        public Color HeaderSelectionUnderlineColor
        {
            get { return (Color)GetValue(HeaderSelectionUnderlineColorProperty); }
            set { SetValue(HeaderSelectionUnderlineColorProperty, value); }
        }
        private static void HeaderSelectionUnderlineColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderSelectionUnderlineColor = (Color)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderSelectionUnderlineColorProperty = BindableProperty.Create(nameof(HeaderSelectionUnderlineColor), typeof(Color), typeof(TabViewControl), Color.White, BindingMode.Default, null, HeaderSelectionUnderlineColorChanged);
        #endregion

        #region HeaderSelectionUnderlineThickness
        public double HeaderSelectionUnderlineThickness
        {
            get { return (double)GetValue(HeaderSelectionUnderlineThicknessProperty); }
            set { SetValue(HeaderSelectionUnderlineThicknessProperty, value); }
        }
        private static void HeaderSelectionUnderlineThicknessChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderSelectionUnderlineThickness = (double)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderSelectionUnderlineThicknessProperty = BindableProperty.Create(nameof(HeaderSelectionUnderlineThickness), typeof(double), typeof(TabViewControl), (double)5, BindingMode.Default, null, HeaderSelectionUnderlineThicknessChanged);
        #endregion

        #region HeaderSelectionUnderlineWidth
        public double HeaderSelectionUnderlineWidth
        {
            get { return (double)GetValue(HeaderSelectionUnderlineWidthProperty); }
            set { SetValue(HeaderSelectionUnderlineWidthProperty, value); }
        }
        private static void HeaderSelectionUnderlineWidthChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderSelectionUnderlineWidth = (double)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderSelectionUnderlineWidthProperty = BindableProperty.Create(nameof(HeaderSelectionUnderlineWidth), typeof(double), typeof(TabViewControl), (double)0, BindingMode.Default, null, HeaderSelectionUnderlineWidthChanged);
        #endregion

        #region HeaderTabTextFontSize
        public double HeaderTabTextFontSize
        {
            get { return (double)GetValue(HeaderTabTextFontSizeProperty); }
            set { SetValue(HeaderTabTextFontSizeProperty, value); }
        }
        private static void HeaderTabTextFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderTabTextFontSize = (double)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderTabTextFontSizeProperty = BindableProperty.Create(nameof(HeaderTabTextFontSize), typeof(double), typeof(TabViewControl), (double)14, BindingMode.Default, null, HeaderTabTextFontSizeChanged);
        #endregion

        #region HeaderTabTextFontFamily
        public string HeaderTabTextFontFamily
        {
            get { return (string)GetValue(HeaderTabTextFontFamilyProperty); }
            set { SetValue(HeaderTabTextFontFamilyProperty, value); }
        }
        private static void HeaderTabTextFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderTabTextFontFamily = (string)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderTabTextFontFamilyProperty = BindableProperty.Create(nameof(HeaderTabTextFontFamily), typeof(string), typeof(TabViewControl), null, BindingMode.Default, null, HeaderTabTextFontFamilyChanged);
        #endregion

        #region HeaderTabTextFontAttributes
        public FontAttributes HeaderTabTextFontAttributes
        {
            get { return (FontAttributes)GetValue(HeaderTabTextFontAttributesProperty); }
            set { SetValue(HeaderTabTextFontAttributesProperty, value); }
        }
        private static void HeaderTabTextFontAttributesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                foreach (var tab in tabViewControl.ItemSource)
                {
                    tab.HeaderTabTextFontAttributes = (FontAttributes)newValue;
                }
            }
        }
        public readonly BindableProperty HeaderTabTextFontAttributesProperty = BindableProperty.Create(nameof(HeaderTabTextFontAttributes), typeof(FontAttributes), typeof(TabViewControl), FontAttributes.None, BindingMode.Default, null, HeaderTabTextFontAttributesChanged);
        #endregion

        #region TabItems
        public static BindableProperty TabItemsProperty = BindableProperty.Create(nameof(TabItems), typeof(IList<TabItem>), typeof(TabViewControl), null, propertyChanged: OnTabItemsChanged);
        private static void OnTabItemsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl)
            {
                tabViewControl.Initialize(tabViewControl.TabItems, tabViewControl.SelectedTabIndex);
            }
        }
        public IList<TabItem> TabItems
        {
            get => (IList<TabItem>)GetValue(TabItemsProperty);
            set { SetValue(TabItemsProperty, value); }
        }
        #endregion

        #region TabSizeOption
        public static BindableProperty TabSizeOptionProperty = BindableProperty.Create(nameof(TabSizeOption), typeof(GridLength), typeof(TabViewControl), default(GridLength), propertyChanged: OnTabSizeOptionChanged);
        private static void OnTabSizeOptionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl._headerContainerGrid != null && tabViewControl.ItemSource != null)
            {
                foreach (var tabContainer in tabViewControl._headerContainerGrid.ColumnDefinitions)
                {
                    tabContainer.Width = (GridLength)newValue;
                }
            }
        }
        public GridLength TabSizeOption
        {
            get => (GridLength)GetValue(TabSizeOptionProperty);
            set { SetValue(TabSizeOptionProperty, value); }
        }
        #endregion

        #region SelectedTabIndex
        public static BindableProperty SelectedTabIndexProperty = BindableProperty.Create(nameof(SelectedTabIndex), typeof(int), typeof(TabViewControl), 0, propertyChanged: OnSelectedTabIndexChanged);
        private static void OnSelectedTabIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TabViewControl tabViewControl && tabViewControl.ItemSource != null)
            {
                tabViewControl.SetPosition((int)newValue);
            }
        }
        public int SelectedTabIndex
        {
            get => (int)GetValue(SelectedTabIndexProperty);
            set { SetValue(SelectedTabIndexProperty, value); }
        }
        #endregion

        public void SetPosition(int position, bool initialRun = false)
        {
            if (SelectedTabIndex == position && !initialRun)
            {
                return;
            }
            int oldPosition = SelectedTabIndex;

            var positionChangingArgs = new PositionChangingEventArgs()
            {
                Canceled = false,
                NewPosition = position,
                OldPosition = oldPosition
            };
            OnPositionChanging(ref positionChangingArgs);

            if (positionChangingArgs != null && positionChangingArgs.Canceled)
            {
                return;
            }

            if (position >= 0 && position < ItemSource.Count)
            {
                for (int i = 0; i < ItemSource.Count; i++)
                {
                    ItemSource[i].IsCurrent = i == position;
                }

                _carouselView.PositionSelected -= _carouselView_PositionSelected;
                _carouselView.Position = position;
                _carouselView.PositionSelected += _carouselView_PositionSelected;

                SelectedTabIndex = position;
            }

            var positionChangedArgs = new PositionChangedEventArgs()
            {
                NewPosition = SelectedTabIndex,
                OldPosition = oldPosition
            };
            OnPositionChanged(positionChangedArgs);
        }

        public void SelectNext()
        {
            SetPosition(SelectedTabIndex + 1);
        }

        public void SelectPrevious()
        {
            SetPosition(SelectedTabIndex - 1);
        }

        public void SelectFirst()
        {
            SetPosition(0);
        }

        public void SelectLast()
        {
            SetPosition(ItemSource.Count - 1);
        }

        public void AddTab(TabItem tab, int position = 0, bool selectNewPosition = false)
        {
            ItemSource.Insert(position, tab);
            if (selectNewPosition)
            {
                SelectedTabIndex = position;
            }
        }

        public void RemoveTab(int position = 0)
        {
            ItemSource.RemoveAt(position);

            if (position > 0)
            {
                SelectedTabIndex = position - 1;
            }
        }
    }

    public class TabItem : ObservableBase
    {
        public TabItem()
        {
            //Parameterless constructor required for xaml instantiation.
        }

        public TabItem(string headerText, View content)
        {
            _headerText = headerText;
            _content = content;
        }

        private string _headerText;
        public string HeaderText
        {
            get { return _headerText; }
            set { SetProperty(ref _headerText, value); }
        }

        private View _content;
        public View Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get { return _isCurrent; }
            set { SetProperty(ref _isCurrent, value); }
        }

        private Color _headerTextColor;
        public Color HeaderTextColor
        {
            get { return _headerTextColor; }
            set { SetProperty(ref _headerTextColor, value); }
        }

        private Color _headerSelectionUnderlineColor;
        public Color HeaderSelectionUnderlineColor
        {
            get { return _headerSelectionUnderlineColor; }
            set { SetProperty(ref _headerSelectionUnderlineColor, value); }
        }

        private double _headerSelectionUnderlineThickness;
        public double HeaderSelectionUnderlineThickness
        {
            get { return _headerSelectionUnderlineThickness; }
            set { SetProperty(ref _headerSelectionUnderlineThickness, value); }
        }

        private double _headerSelectionUnderlineWidth;
        public double HeaderSelectionUnderlineWidth
        {
            get { return _headerSelectionUnderlineWidth; }
            set { SetProperty(ref _headerSelectionUnderlineWidth, value); }
        }

        private double _headerTabTextFontSize;
        public double HeaderTabTextFontSize
        {
            get { return _headerTabTextFontSize; }
            set { SetProperty(ref _headerTabTextFontSize, value); }
        }

        private string _headerTabTextFontFamily;
        public string HeaderTabTextFontFamily
        {
            get { return _headerTabTextFontFamily; }
            set { SetProperty(ref _headerTabTextFontFamily, value); }
        }

        private FontAttributes _headerTabTextFontAttributes;
        public FontAttributes HeaderTabTextFontAttributes
        {
            get { return _headerTabTextFontAttributes; }
            set { SetProperty(ref _headerTabTextFontAttributes, value); }
        }
    }

    public class ObservableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}