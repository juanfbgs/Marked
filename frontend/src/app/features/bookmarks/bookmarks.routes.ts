import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';

export const BOOKMARKS_ROUTES: Routes = [
    {
        path: '',
        loadComponent: () => import('./bookmark-list/bookmark-list').then((m) => m.BookmarkList),
        canActivate: [authGuard],
    },
];