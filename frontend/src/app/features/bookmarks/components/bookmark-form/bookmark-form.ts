import { Component, effect, inject, input, output, signal } from '@angular/core';
import { form, FormField, FormRoot, pattern, required } from '@angular/forms/signals';
import { Bookmark } from '@core/models/bookmark.model';
import { BookmarkService } from '@core/services/bookmark.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-bookmark-form',
  imports: [FormRoot, FormField],
  templateUrl: './bookmark-form.html',
  styleUrl: './bookmark-form.css',
})
export class BookmarkForm {
  private bookmarkService = inject(BookmarkService);

  created = output<Bookmark>();
  cancelled = output<void>();

  model = signal({ title: '', url: '' });
  image = signal<File | null>(null);

  bookmark = input<Bookmark | null>(null);
  updated = output<string>();

  constructor() {
    effect(() => {
      const b = this.bookmark();
      if (b) {
        this.model.set({ title: b.title, url: b.url });
      }
    });
  }

  form = form(this.model, (f) => {
    required(f.title, { message: 'Title is required' });
    required(f.url, { message: 'URL is required' });
    pattern(f.url, /^https?:\/\/[^\s/$.?#].[^\s]*$/i, {
      message: 'Enter a valid URL (e.g. https://example.com)',
    });
  }, {
    submission: {
      action: async () => {
        const b = this.bookmark();
        if (b) {
          await firstValueFrom(this.bookmarkService.update(b.id, this.model(), this.image() ?? undefined));
          this.updated.emit(b.id);
        } else {
          const created = await firstValueFrom(this.bookmarkService.create(this.model(), this.image() ?? undefined));
          this.created.emit(created);
        }
      },
    },
  });

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      if (file.size > 10 * 1024 * 1024) return; // 10MB limit
      this.image.set(file);
    }
  }
}
