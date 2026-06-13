import { Component, inject, signal } from '@angular/core';
import { User } from '@core/models/auth.model';
import { Bookmark } from '@core/models/bookmark.model';
import { BookmarkService } from '@core/services/bookmark.service';
import { ProfileService } from '@core/services/profile.service';
import { finalize, switchMap, tap } from 'rxjs';
import { BookmarkForm } from '../components/bookmark-form/bookmark-form';

@Component({
  selector: 'app-bookmark-list',
  imports: [BookmarkForm],
  templateUrl: './bookmark-list.html',
  styleUrl: './bookmark-list.css',
})
export class BookmarkList {
  private profileService = inject(ProfileService);
  private bookmarkService = inject(BookmarkService);

  profile = signal<User | null>(null);
  bookmarks = signal<Bookmark[]>([]);
  editingBookmark = signal<Bookmark | null>(null);
  deletingBookmarkId = signal<string | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  showForm = signal(false);

  constructor() {
    this.loadData();
  }

  private loadData(): void {
    this.profileService.getProfile().pipe(
      tap({ next: (user) => this.profile.set(user) }),
      switchMap(() => this.bookmarkService.getAll()),
      finalize(() => this.isLoading.set(false)),
    ).subscribe({
      next: (res) => this.bookmarks.set(res ?? []),
      error: () => this.error.set('Failed to load data'),
    });
  }

  startEdit(bookmark: Bookmark) {
    this.editingBookmark.set(bookmark);
  }

  deleteBookmark(id: string): void {
    this.bookmarkService.delete(id).subscribe({
      next: () => this.bookmarks.update((list) => list.filter((b) => b.id !== id)),
      error: () => this.error.set('Failed to delete bookmark'),
    });
  }

  onBookmarkCreated(bookmark: Bookmark) {
    this.showForm.set(false);
    this.bookmarks.update((list) => [bookmark, ...list]);
  }

  onBookmarkUpdated(id: string) {
    this.editingBookmark.set(null);
    this.loadData();
  }

  confirmDelete(id: string) {
    this.deletingBookmarkId.set(id);
  }

  onDeleteConfirmed() {
    const id = this.deletingBookmarkId();
    if (!id) return;
    this.bookmarkService.delete(id).subscribe({
      next: () => {
        this.bookmarks.update(list => list.filter(b => b.id !== id));
        this.deletingBookmarkId.set(null);
      },
      error: () => this.error.set('Failed to delete'),
    });
  }

}
