namespace ArticleCrawler.Api
{
    public class ArticleEntity
    {
        public int ID { get; set; }
        public string Url { get; set; }
        public string Summary { get; set; }
        public string MetaKeyWords { get; set; }
        public string MetaDescription { get; set; }
        public int? Counter { get; set; }
        public string ArticleContent { get; set; }
        public string ImageThumbUrl { get; set; }
        public string ImageUrl { get; set; }
        public int? ArticleTypeID { get; set; }
        public DateTime? PublishDate { get; set; }
        public bool IsPublish { get; set; }
        public int CultureID { get; set; }

        public virtual string Title { get; set; }

        public virtual string Description { get; set; }

        public virtual int? Status { get; set; }

        public virtual string CreateBy { get; set; }

        public virtual string CreatebyName { get; set; }

        public virtual DateTime? CreateDate { get; set; }

        public virtual string LastUpdateBy { get; set; }

        public virtual string LastUpdateByName { get; set; }

        public virtual DateTime? LastUpdateDate { get; set; }
    }
}