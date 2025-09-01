import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchCurrentUser } from "../../store/slices/userSlice";
import { PropsWithChildren } from "react";

type protectedRouteProps = PropsWithChildren;

const ProtectedRoute = ({ children }: protectedRouteProps) => {
  const { authenticated } = useSelector((state: RootState) => state.user);
  // const location = useLocation();
  const dispatch = useAppDispatch();

  const userId = localStorage.getItem("userId");

  if (userId !== null && !authenticated) {
    dispatch(fetchCurrentUser(userId));
  }
  // else if (!authenticated) {
  //   return <Navigate to="/login" state={{ from: location }} />;
  // }

  return children;
};

export default ProtectedRoute;
