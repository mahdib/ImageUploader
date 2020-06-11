namespace ImageUploader.Helpers
{
    public class ImageSettings
    {
        public int HighResWidth { get; set; }
        public int HighResHeight { get; set; }
        public long MinSize { get; set; }
        public long MaxSize { get; set; }
        public int ThumbWidth { get; set; }
        public int ThumbHeight { get; set; }
        public long ThumbSize { get; set; }
    }
}