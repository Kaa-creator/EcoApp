namespace EcoAppMobile;

public partial class ArticleDetailPage : ContentPage
{
    public ArticleDetailPage(ArticleViewModel article)
    {
        InitializeComponent();

        CategoryLabel.Text = article.Category;
        TitleLabel.Text = article.Title;
        ContentLabel.Text = article.Content;
    }
}