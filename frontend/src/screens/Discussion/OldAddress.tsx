import { Navigate, useParams, useSearchParams } from "react-router-dom";

// The Discussion used to have two screens of its own. Their addresses now open the
// section at the foot of the Period's page, on the same page of Topics or the same Topic.
export default function OldAddress() {
  const { id, topicId } = useParams<{ id: string; topicId?: string }>();
  const [searchParams] = useSearchParams();
  const params = new URLSearchParams();
  const page = searchParams.get("page");
  if (topicId) params.set("topic", topicId);
  else if (page) params.set("page", page);
  const search = params.toString();
  return <Navigate to={`/learning/${id}${search ? `?${search}` : ""}#rasprava`} replace />;
}
