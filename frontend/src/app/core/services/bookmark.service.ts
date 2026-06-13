import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { Bookmark, BookmarkListResponse } from "@core/models/bookmark.model";
import { environment } from "@envs/environment.development";
import { Observable } from "rxjs";

@Injectable({
    providedIn: 'root',
})
export class BookmarkService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly baseUrl = `${environment.API_URL}/bookmarks`;

    getAll(): Observable<Bookmark[]> {
        return this.http.get<Bookmark[]>(this.baseUrl);
    }

    getById(id: string): Observable<Bookmark> {
        return this.http.get<Bookmark>(`${this.baseUrl}/${id}`);
    }

    create(data: { title: string; url: string }, image?: File): Observable<Bookmark> {
        const formData = new FormData();
        formData.append('title', data.title);
        formData.append('url', data.url);
        if (image) formData.append('image', image);
        return this.http.post<Bookmark>(this.baseUrl, formData);
    }

    update(id: string, data: { title: string; url: string }, image?: File): Observable<void> {
        const formData = new FormData();
        formData.append('title', data.title);
        formData.append('url', data.url);
        if (image) formData.append('image', image);
        return this.http.put<void>(`${this.baseUrl}/${id}`, formData);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}