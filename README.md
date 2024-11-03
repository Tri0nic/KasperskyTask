# WebApplication1
## Политика:

![image](https://github.com/user-attachments/assets/d5bbb68f-2dd1-4206-b965-4a6be74876c3)

## Пример 1:
### Передаем:
<pre>
{
  "paths": [
    "folder1\\exampleReviewTXT.txt",
    "folder1\\services\\exampleReviewMD.md"
  ]
}
</pre>
### Получаем:
<pre>
  "82aa8541-3b05-4685-8cf4-09a606679316": {
  "Status": "Completed",
  "Timestamp": "2024-11-03T13:20:27.3181714Z",
  "Paths": [
    "folder1\\exampleReviewTXT.txt",
    "folder1\\services\\exampleReviewMD.md"
  ],
  "ReviewerFilePath": "reviewers.yaml",
  "Reviewers": [
    "user1",
    "user2"
  ]
}
</pre>

![image](https://github.com/user-attachments/assets/452a837f-76ee-4bf9-b45b-6667cbd520ad)


## Пример 2:
### Передаем:
<pre>
{
  "paths": [
    "folder1\\windows\\exampleReviewMD.md"
  ]
}
</pre>
### Получаем:
<pre>
  "76c3ad31-965c-4827-ad88-87353036ced4": {
    "Status": "Completed",
    "Timestamp": "2024-11-03T12:23:09.1603444Z",
    "Paths": [
      "folder1\\windows\\exampleReviewMD.md"
    ],
    "ReviewerFilePath": "reviewers.yaml",
    "Reviewers": [
      "user1",
      "user2",
      "user3"
    ]
  }
</pre>

![image](https://github.com/user-attachments/assets/acec9b19-e2d8-48b8-b2e6-2b9070c585b2)

