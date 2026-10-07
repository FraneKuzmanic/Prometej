import logo from "/logo.svg";
import { Art, ArtText } from "./index.styled";

// The half of the sign-in and registration screens that is not the form.
export default function AuthArt() {
  return (
    <Art>
      <img className="painting" src="/humanizam-i-predrenesansa.webp" alt="" />
      <ArtText>
        <img src={logo} alt="" />
        <div>
          <h2>Prometej</h2>
          <p>Gradivo hrvatske književnosti i kvizovi za vježbu na jednom mjestu.</p>
        </div>
      </ArtText>
    </Art>
  );
}
