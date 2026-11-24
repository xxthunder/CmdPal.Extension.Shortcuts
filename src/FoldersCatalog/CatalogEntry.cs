namespace FoldersCatalog;

/// <summary>One catalogued file: its path as reached, its file name with extension, and its folder.</summary>
public sealed record CatalogEntry(string FullPath, string Name, string Folder);
