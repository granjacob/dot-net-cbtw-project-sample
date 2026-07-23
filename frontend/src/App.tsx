import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { DashboardPage } from "./pages/DashboardPage";
import { LoginPage } from "./pages/LoginPage";
import { NewRequestPage } from "./pages/NewRequestPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { RequestDetailPage } from "./pages/RequestDetailPage";
import { RequestsPage } from "./pages/RequestsPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="requests" element={<RequestsPage />} />
          <Route path="requests/new" element={<NewRequestPage />} />
          <Route path="requests/:id" element={<RequestDetailPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
