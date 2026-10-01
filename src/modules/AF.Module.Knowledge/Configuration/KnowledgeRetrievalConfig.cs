namespace AnimusForge;

public class KnowledgeRetrievalConfig
{
	public bool IsEnabled { get; set; } = true;

	public bool SemanticFirst { get; set; } = true;

	public int SemanticTopK { get; set; } = 2;

	public float SemanticMinScore { get; set; } = 0.21f;
}
