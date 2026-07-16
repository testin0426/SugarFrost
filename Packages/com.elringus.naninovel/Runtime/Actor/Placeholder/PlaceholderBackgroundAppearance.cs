using System;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public class PlaceholderBackgroundAppearance
    {
        public string Name => name;
        public Color[] Colors => colors;
        public float Speed => speed;
        public float Angle => angle;
        public bool Radial => radial;
        public float TextSize => textSize;
        public Color TextColor => textColor;
        public Vector2 TextSpeed => textSpeed;

        [SerializeField] private string name;
        [SerializeField] private Color[] colors;
        [SerializeField] private float speed;
        [SerializeField] private float angle;
        [SerializeField] private bool radial;
        [SerializeField] private float textSize;
        [SerializeField] private Color textColor;
        [SerializeField] private Vector2 textSpeed;

        public static PlaceholderBackgroundAppearance WithName (PlaceholderBackgroundAppearance prototype, string name) => new() {
            name = name,
            colors = prototype.colors.ToArray(),
            speed = prototype.speed,
            angle = prototype.angle,
            radial = prototype.radial,
            textSize = prototype.textSize,
            textColor = prototype.textColor,
            textSpeed = prototype.textSpeed
        };

        public static readonly PlaceholderBackgroundAppearance Black = new() {
            name = "Black",
            colors = new Color[] { new(0, 0, 0) },
            speed = 0,
            angle = 0,
            radial = false,
            textSize = 1,
            textColor = new(0, 0, 0),
            textSpeed = Vector2.zero
        };

        public static readonly PlaceholderBackgroundAppearance White = new() {
            name = "White",
            colors = new Color[] { new(1, 1, 1) },
            speed = 0,
            angle = 0,
            radial = false,
            textSize = 1,
            textColor = new(1, 1, 1),
            textSpeed = Vector2.zero
        };

        public static readonly PlaceholderBackgroundAppearance Light = new() {
            name = "Light",
            colors = new Color[] {
                new(.85f, .85f, .65f),
                new(.85f, .5f, .5f),
                new(.35f, .5f, .85f)
            },
            speed = .05f,
            angle = 45,
            radial = false,
            textSize = 1,
            textColor = new(1, 1, 1, .2f),
            textSpeed = new(-.05f, -.025f)
        };

        public static readonly PlaceholderBackgroundAppearance Dark = new() {
            name = "Dark",
            colors = new Color[] {
                new(.3f, .35f, .6f),
                new(.4f, .3f, .6f),
                new(.5f, .3f, .35f)
            },
            speed = -.05f,
            angle = 45,
            radial = false,
            textSize = 1,
            textColor = new(1, 1, 1, .1f),
            textSpeed = new(.05f, .025f)
        };

        public static readonly PlaceholderBackgroundAppearance City = new() {
            name = "City",
            colors = new Color[] {
                new(.26f, .30f, .30f),
                new(.30f, .40f, .44f),
                new(.62f, .62f, .32f),
                new(.31f, .45f, .15f),
                new(.34f, .34f, .34f),
                new(.72f, .71f, .5f)
            },
            speed = .005f,
            angle = 0,
            radial = false,
            textSize = 1,
            textColor = new(1, 1, 1, .15f),
            textSpeed = new(-.1f, .01f)
        };

        public static readonly PlaceholderBackgroundAppearance Desert = new() {
            name = "Desert",
            colors = new Color[] {
                new(.54f, .89f, 1),
                new(1, .96f, .68f),
                new(.86f, .71f, .31f),
                new(.56f, .46f, .24f)
            },
            speed = 0,
            angle = -90,
            radial = false,
            textSize = 1,
            textColor = new(.26f, .18f, .00f, .25f),
            textSpeed = new(.1f, 0)
        };

        public static readonly PlaceholderBackgroundAppearance Snow = new() {
            name = "Snow",
            colors = new Color[] {
                new(.96f, 1, 1),
                new(.76f, .95f, 1),
                new(.44f, .64f, .83f),
                new(.24f, .45f, .56f)
            },
            speed = 0,
            angle = -90,
            radial = false,
            textSize = 1,
            textColor = new(.00f, .21f, .32f, .19f),
            textSpeed = new(0, .05f)
        };

        public static readonly PlaceholderBackgroundAppearance Mist = new() {
            name = "Mist",
            colors = new Color[] {
                new(.16f, .16f, .16f),
                new(.22f, .22f, .22f),
                new(.33f, .33f, .33f),
                new(.46f, .46f, .46f),
                new(.33f, .33f, .33f),
                new(.22f, .22f, .22f)
            },
            speed = .025f,
            angle = 0,
            radial = true,
            textSize = 1,
            textColor = new(.55f, .55f, .55f, .15f),
            textSpeed = new(-.1f, .05f)
        };

        public static readonly PlaceholderBackgroundAppearance Cosmos = new() {
            name = "Cosmos",
            colors = new Color[] {
                new(.12f, .00f, .12f),
                new(.10f, .00f, .18f),
                new(.11f, .02f, .28f),
                new(.12f, .04f, .42f),
                new(.26f, .35f, .56f),
                new(.58f, .47f, .01f)
            },
            speed = -.1f,
            angle = 0,
            radial = true,
            textSize = 1,
            textColor = new(.6f, .6f, .6f, .1f),
            textSpeed = new(-.05f, .025f)
        };
    }
}
