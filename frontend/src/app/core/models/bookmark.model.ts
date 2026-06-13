export interface Bookmark {
  id: string;
  title: string;
  url: string;
  imageUrl: string | null;
  createdAt: string;
}

export interface BookmarkListResponse {
  bookmarks: Bookmark[];
}