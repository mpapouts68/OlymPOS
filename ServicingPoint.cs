using System.ComponentModel;

public class ServicingPoint : INotifyPropertyChanged
{
    private string _description;
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
                // Also update FullDescription as it depends on Description
                OnPropertyChanged(nameof(FullDescription));
            }
        }
    }

    private bool _active;
    public bool Active
    {
        get => _active;
        set
        {
            if (_active != value)
            {
                _active = value;
                OnPropertyChanged(nameof(Active));
            }
        }
    }

    private int _postID;
    public int PostID
    {
        get => _postID;
        set
        {
            if (_postID != value)
            {
                _postID = value;
                OnPropertyChanged(nameof(PostID));
            }
        }
    }

    private int _activeOrderID;
    public int ActiveOrderID 
    {
        get => _activeOrderID;
        set
        {
            if (_activeOrderID != value)
            {
                _activeOrderID = value;
                OnPropertyChanged(nameof(ActiveOrderID));
            }
        }
    }

    private int _postNumber;
    public int PostNumber
    {
        get => _postNumber;
        set
        {
            if (_postNumber != value)
            {
                _postNumber = value;
                OnPropertyChanged(nameof(PostNumber));
                // Also update FullDescription as it depends on PostNumber
                OnPropertyChanged(nameof(FullDescription));
            }
        }
    }

    private bool _reserved;
    public bool Reserved
    {
        get => _reserved;
        set
        {
            if (_reserved != value)
            {
                _reserved = value;
                OnPropertyChanged(nameof(Reserved));
            }
        }
    }

    // Read-only property combining Description and PostNumber
    public string FullDescription => $"{Description} {PostNumber}";

    // Implementation of INotifyPropertyChanged interface
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}




